using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Identity;      // for IdentityRole
using TmsApi.Infrastructure.Identity;     //
using Microsoft.Extensions.Caching.Hybrid;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Services;
using TmsApi.Application.Interfaces;
using TmsApi.Api.Middleware; 
using Scalar.AspNetCore;
using TmsApi.Api.Filters;
using Asp.Versioning;
using MediatR;
using FluentValidation;
using TmsApi.Application.Behaviors;
using TmsApi.Application.Enrollments.Commands;
using TmsApi.Api.ExceptionHandlers;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Antiforgery;   // ← add this line
using Microsoft.AspNetCore.RateLimiting;
using TmsApi.Api.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Channels;
using TmsApi.Application.Transcripts;
using TmsApi.Infrastructure.Transcripts;
using TmsApi.Infrastructure.Workers;
using TmsApi.Application.Hubs;
var builder = WebApplication.CreateBuilder(args);

// 1. Services
builder.Services.AddAuthentication("Training")
    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, TrainingAuthHandler>("Training", null)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddControllers(options =>
{
    options.Filters.Add<AuditLogFilter>();
});
builder.Services.AddProblemDetails();
// CORS: named policy, origins driven by configuration
var allowedOrigins = builder.Configuration
    .GetSection("AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:4200"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("TmsClient", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials() // needed later for HttpOnly auth cookies (Session 2)
              .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
    });
});
//Session 2: Antiforgery for XSRF double-submit protection
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";
});

// Session 1 Exercise 2: MediatR + Validation pipeline
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(EnrollStudentHandler).Assembly));
builder.Services.AddValidatorsFromAssembly(typeof(EnrollStudentValidator).Assembly);

// LoggingBehavior FIRST it must wrap ValidationBehavior
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Session 3: Transcript status store, channel, worker, SignalR
builder.Services.AddSingleton<ITranscriptStatusStore, InMemoryTranscriptStatusStore>();
builder.Services.AddSingleton(Channel.CreateBounded<TranscriptRequest>(
    new BoundedChannelOptions(100) { FullMode = BoundedChannelFullMode.Wait }));
builder.Services.AddHostedService<TranscriptWorker>();
builder.Services.AddSignalR();
builder.Services.AddOpenApi("v1", options =>
{
    options.ShouldInclude = description =>
        description.GroupName == "v1";
});
builder.Services.AddOpenApi("v2", options =>
{
    options.ShouldInclude = description =>
        description.GroupName == "v2";
});
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("X-Api-Version"));
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});
builder.Services.AddDbContext<TmsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("TmsDatabase"))
           .LogTo(Console.WriteLine, LogLevel.Information)
           .EnableSensitiveDataLogging());

// Session 1 (M11): ASP.NET Core Identity — password policy + lockout
builder.Services.AddIdentityCore<TmsUser>(options =>
{
    options.Password.RequiredLength = 12;
    options.Password.RequireUppercase = true;
    options.Password.RequireDigit = true;
    options.Password.RequireNonAlphanumeric = true;

    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.AllowedForNewUsers = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<TmsDbContext>();
// 2. DI Registration
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<ICachedCourseService, CachedCourseService>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Services.AddScoped<TokenService>();
// 3. Options Registration (Exercise 3)
builder.Services.AddOptions<PaymentOptions>()
    .Bind(builder.Configuration.GetSection("Payments"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// 4. DI Validation
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});
builder.Services.AddHybridCache(options =>
{
    options.DefaultEntryOptions = new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromMinutes(10),
        LocalCacheExpiration = TimeSpan.FromMinutes(2)
    };
});
// Production-only — leave commented in lab
// builder.Services.AddStackExchangeRedisCache(options =>
// {
//     options.Configuration = builder.Configuration.GetConnectionString("Redis");
//     options.InstanceName = "tms:";
// });
// builder.Services.AddHybridCache();
builder.Services.AddRateLimiter(options =>
{
    options.AddConcurrencyLimiter("transcripts", opt =>
{
    opt.PermitLimit = 5;
    opt.QueueLimit = 20;
    opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
});
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var (partitionKey, tier) = ApiKeyResolver.Resolve(httpContext);

        return tier switch
        {
            ApiKeyTier.Paid => RateLimitPartition.GetTokenBucketLimiter(
                partitionKey: $"paid:{partitionKey}",
                factory: _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 200,
                    TokensPerPeriod = 100,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }),
            ApiKeyTier.Free => RateLimitPartition.GetTokenBucketLimiter(
                partitionKey: $"free:{partitionKey}",
                factory: _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 30,
                    TokensPerPeriod = 10,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }),
            _ => RateLimitPartition.GetTokenBucketLimiter(
                partitionKey: $"anon:{partitionKey}",
                factory: _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 10,
                    TokensPerPeriod = 5,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                    QueueLimit = 0,
                    AutoReplenishment = true
                })
        };
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, ct) =>
    {
        var retryAfter = "10";
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var ts))
            retryAfter = ((int)ts.TotalSeconds).ToString();

        context.HttpContext.Response.Headers.RetryAfter = retryAfter;
        context.HttpContext.Response.ContentType = "application/problem+json";

        await context.HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Title = "Rate limit exceeded",
            Detail = $"Too many requests. Retry after {retryAfter} seconds.",
            Status = StatusCodes.Status429TooManyRequests,
            Type = "https://tms.local/errors/rate_limit_exceeded"
        }, ct);
    };
});
var app = builder.Build();
/*var demo = new TmsApi.Infrastructure.Services.CryptoDemoService();
var h1 = demo.HashUserPassword("Password123!");
var h2 = demo.HashUserPassword("Password123!");
Console.WriteLine($"Hash 1: {h1}");
Console.WriteLine($"Hash 2: {h2}");
Console.WriteLine($"Match1: {demo.VerifyUserPassword("Password123!", h1)}");
Console.WriteLine($"Match2: {demo.VerifyUserPassword("Password123!", h2)}");*/
// 5. Middleware Pipeline
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseRouting();
app.UseCors("TmsClient"); 
app.UseAuthentication();
app.UseAuthorization();
// Session 2: Issue readable XSRF-TOKEN cookie for authenticated sessions
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true || context.Request.Cookies.ContainsKey("tms_auth"))
    {
        var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();
        var tokens = antiforgery.GetAndStoreTokens(context);
        context.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken!,
            new CookieOptions
            {
                HttpOnly = false, // MUST be false — Angular JS needs to read this
                Secure = !builder.Environment.IsDevelopment(),
                SameSite = SameSiteMode.Strict
            });
    }
    await next(context);
});
app.UseMiddleware<V1DeprecationMiddleware>();
app.UseRateLimiter();
app.MapControllers();
app.MapHub<TmsHub>("/hubs/tms").RequireCors("TmsClient");// 7. Environment-aware configuration
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
{
    options.WithTitle("TMS API Reference")
           .WithTheme(ScalarTheme.DeepSpace)
           .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    options
        .AddDocument("v1", "API Version 1.0")
        .AddDocument("v2", "API Version 2.0");
});
}
// 6. Endpoints
app.MapGet("/api/assessments/results", () => Results.Ok(new
{
    courseCode = "CS-101",
    studentId = "S-001",
    letterGrade = "A"
})).RequireAuthorization();

app.MapGet("/api/error", () =>
{
    throw new Exception("Simulated database failure for ProblemDetails testing");
});

// Seed test data at startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<TmsDbContext>();
    context.Database.Migrate();

    // ONE-TIME FIX: Set empty Status values to "Pending" (from AddEnrollmentStatus migration default)
    var emptyStatusEnrollments = context.Enrollments.Where(e => e.Status == "").ToList();
    if (emptyStatusEnrollments.Any())
    {
        foreach (var enrollment in emptyStatusEnrollments)
        {
            enrollment.Status = "Pending";
        }
        context.SaveChanges();
    }

    if (!context.Students.Any()){
        var students = new List<Student>
        {
            new() { RegistrationNumber = "TMS-2026-0001", Name = "Alice Smith", GPA = 3.8m, IsActive = true },
            new() { RegistrationNumber = "TMS-2026-0002", Name = "Bob Jones", GPA = 2.9m, IsActive = true },
            new() { RegistrationNumber = "TMS-2026-0003", Name = "Charlie Brown", GPA = 3.4m, IsActive = false },
            new() { RegistrationNumber = "TMS-2026-0004", Name = "Diana Prince", GPA = 3.9m, IsActive = true },
            new() { RegistrationNumber = "TMS-2026-0005", Name = "Evan Wright", GPA = 2.5m, IsActive = true }
        };
        context.Students.AddRange(students);

    }
}
// 8. M6 Session 2: Seed 25 courses (Development only, idempotent)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<TmsDbContext>();
    await DataSeeder.SeedAsync(context);
}

// M11 Session 2: Seed a test admin user (Development only, idempotent)
if (app.Environment.IsDevelopment())
{
    using var identityScope = app.Services.CreateScope();
    var userManager = identityScope.ServiceProvider.GetRequiredService<UserManager<TmsUser>>();
    var roleManager = identityScope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        await roleManager.CreateAsync(new IdentityRole("Admin"));
    }

    var existingAdmin = await userManager.FindByNameAsync("admin");
    if (existingAdmin == null)
    {
        var adminUser = new TmsUser
        {
            UserName = "admin",
            Email = "admin@tms.local",
            FirstName = "System",
            LastName = "Admin",
            EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(adminUser, "Password123!");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
}
app.Run();