using Tickify.API.Middleware;

namespace Tickify.API.Extensions;

public static class HttpContextExtensions
{
    /// <summary>The correlation id of the current request (falls back to the ASP.NET trace id).</summary>
    public static string GetCorrelationId(this HttpContext context) =>
        context.Items[CorrelationIdMiddleware.ItemKey] as string ?? context.TraceIdentifier;
}
