using Tickify.API.Extensions;

namespace Tickify.API.Middleware;

/// <summary>
/// Turns any unhandled exception into the same JSON error shape used everywhere else
/// (status, title, detail, correlationId) and logs it as Error. No stack trace leaves Development.
/// </summary>
public class ExceptionHandlingMiddleware
{
    public const int ClientClosedRequest = 499;

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IProblemDetailsService _problemDetails;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IProblemDetailsService problemDetails,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _problemDetails = problemDetails;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // The client went away; that is not a server error.
            using var scope = _logger.BeginCorrelationScope(context.GetCorrelationId());
            _logger.LogDebug("Request {Method} {Path} was cancelled by the client", context.Request.Method, context.Request.Path.Value);
            context.Response.StatusCode = ClientClosedRequest;
        }
        catch (Exception ex)
        {
            // This middleware is outside the correlation middleware, so the id (kept in HttpContext.Items) is re-opened here.
            using var scope = _logger.BeginCorrelationScope(context.GetCorrelationId());
            _logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path.Value);

            if (context.Response.HasStarted)
                throw;

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await _problemDetails.WriteAsync(new ProblemDetailsContext
            {
                HttpContext = context,
                ProblemDetails =
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Internal Server Error",
                    Detail = _environment.IsDevelopment() ? ex.ToString() : "An unexpected error occurred."
                }
            });
        }
    }
}
