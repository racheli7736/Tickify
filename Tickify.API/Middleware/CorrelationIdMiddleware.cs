using System.Diagnostics;

namespace Tickify.API.Middleware;

/// <summary>
/// Gives every request a correlation id (from the X-Correlation-Id header or a new Guid), returns it in the
/// response, puts it in the logging scope of the whole request and logs one line per request.
/// </summary>
public class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";
    public const string ItemKey = "CorrelationId";
    private const int MaxIncomingLength = 64;

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context);
        context.Items[ItemKey] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (_logger.BeginScope("CorrelationId:{CorrelationId}", correlationId))
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                await _next(context);
            }
            finally
            {
                _logger.LogInformation(
                    "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs} ms",
                    context.Request.Method, context.Request.Path.Value, context.Response.StatusCode, stopwatch.ElapsedMilliseconds);
            }
        }
    }

    // A caller-supplied id is only trusted if it is short and made of safe characters (no log injection).
    private static string ResolveCorrelationId(HttpContext context)
    {
        var incoming = context.Request.Headers[HeaderName].FirstOrDefault();

        return !string.IsNullOrWhiteSpace(incoming)
               && incoming.Length <= MaxIncomingLength
               && incoming.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.')
            ? incoming
            : Guid.NewGuid().ToString();
    }
}
