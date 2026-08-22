using Microsoft.AspNetCore.Identity.EntityFrameworkCore;   // ← new
using Microsoft.EntityFrameworkCore;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Identity;                       // ← new

namespace TmsApi.Infrastructure.Persistence;

public class TmsDbContext : IdentityDbContext<TmsUser>       // ← was: DbContext
{
    public TmsDbContext(DbContextOptions<TmsDbContext> options)
        : base(options) { }

    public DbSet<Student> Students => Set<Student>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);   // ← new — required for Identity's own tables
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TmsDbContext).Assembly);
    }
}