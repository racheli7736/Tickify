using Tickify.API.Middleware;

namespace Tickify.API.Extensions;

public static class HttpContextExtensions
{
    /// <summary>Makes every log line written inside the returned scope carry the correlation id.</summary>
    public static IDisposable? BeginCorrelationScope(this ILogger logger, string correlationId) =>
        logger.BeginScope("CorrelationId:{CorrelationId}", correlationId);

    /// <summary>The correlation id of the current request (falls back to the ASP.NET trace id).</summary>
    public static string GetCorrelationId(this HttpContext context) =>
        context.Items[CorrelationIdMiddleware.ItemKey] as string ?? context.TraceIdentifier;
}
