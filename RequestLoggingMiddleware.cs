using System.Diagnostics;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Generate correlation id
        var correlationId = Guid.NewGuid().ToString("N")[..8];

        // Set header BEFORE next (important!)
        context.Response.Headers["X-Correlation-Id"] = correlationId;

        // Start timer
        var stopwatch = Stopwatch.StartNew();

        // Log entry
        _logger.LogInformation(
            "Request {Method} {Path} [{CorrelationId}]",
            context.Request.Method,
            context.Request.Path,
            correlationId);

        // Pass to next middleware
        await _next(context);

        // Stop timer
        stopwatch.Stop();

        // Log exit
        _logger.LogInformation(
            "Response {StatusCode} {ElapsedMs}ms [{CorrelationId}]",
            context.Response.StatusCode,
            stopwatch.ElapsedMilliseconds,
            correlationId);
    }
}