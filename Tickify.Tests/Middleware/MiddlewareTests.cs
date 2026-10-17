using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tickify.API.Extensions;
using Tickify.API.Middleware;
using Tickify.Tests.Helpers;

namespace Tickify.Tests.Middleware;

public class MiddlewareTests
{
    private readonly CapturingLoggerProvider _logs = new();

    private static ServiceProvider BuildServices(string environment = "Production") =>
        new ServiceCollection()
            .AddLogging()
            .AddSingleton<IHostEnvironment>(new FakeEnvironment(environment))
            .AddProblemDetails(o => o.CustomizeProblemDetails =
                ctx => ctx.ProblemDetails.Extensions["correlationId"] = ctx.HttpContext.GetCorrelationId())
            .BuildServiceProvider();

    private static DefaultHttpContext NewContext(ServiceProvider services, string? correlationHeader = null)
    {
        var context = new DefaultHttpContext { RequestServices = services };
        context.Response.Body = new MemoryStream();
        if (correlationHeader is not null)
            context.Request.Headers[CorrelationIdMiddleware.HeaderName] = correlationHeader;
        return context;
    }

    /// <summary>The pipeline in the order Program.cs registers it: ExceptionHandling first, then CorrelationId, then the app.</summary>
    private RequestDelegate Pipeline(ServiceProvider services, RequestDelegate app)
    {
        var loggers = _logs.CreateFactory();
        RequestDelegate correlation = new CorrelationIdMiddleware(app, loggers.CreateLogger<CorrelationIdMiddleware>()).InvokeAsync;
        return new ExceptionHandlingMiddleware(
            correlation, loggers.CreateLogger<ExceptionHandlingMiddleware>(),
            services.GetRequiredService<IProblemDetailsService>(), services.GetRequiredService<IHostEnvironment>()).InvokeAsync;
    }

    private static JsonElement ReadJson(HttpContext context)
    {
        context.Response.Body.Position = 0;
        return JsonDocument.Parse(context.Response.Body).RootElement;
    }

    // ---------- correlation id ----------

    [Fact]
    public async Task CorrelationId_UsesIncomingHeader_AndEchoesItInTheResponse()
    {
        using var services = BuildServices();
        var context = NewContext(services, "abc-123");

        await Pipeline(services, _ => Task.CompletedTask)(context);

        Assert.Equal("abc-123", context.Response.Headers[CorrelationIdMiddleware.HeaderName]);
        Assert.Equal("abc-123", context.GetCorrelationId());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("bad value\r\nwith newline")]
    public async Task CorrelationId_GeneratesGuid_WhenHeaderMissingOrUnsafe(string? incoming)
    {
        using var services = BuildServices();
        var context = NewContext(services, incoming);

        await Pipeline(services, _ => Task.CompletedTask)(context);

        Assert.True(Guid.TryParse(context.GetCorrelationId(), out _));
        Assert.Equal(context.GetCorrelationId(), context.Response.Headers[CorrelationIdMiddleware.HeaderName]);
    }

    [Fact]
    public async Task EveryRequestIsLoggedOnce_WithTheCorrelationIdInScope()
    {
        using var services = BuildServices();
        var context = NewContext(services, "req-0");
        context.Request.Method = "GET";
        context.Request.Path = "/api/events";

        await Pipeline(services, c => { c.Response.StatusCode = 200; return Task.CompletedTask; })(context);

        var line = Assert.Single(_logs.Entries, e => e.Level == LogLevel.Information);
        Assert.Contains("GET /api/events responded 200", line.Message);
        Assert.Contains("CorrelationId:req-0", line.Scopes);
    }

    // ---------- unhandled exceptions (exception handling is the OUTERMOST middleware) ----------

    [Fact]
    public async Task UnhandledException_Returns500WithUniformJsonAndCorrelationId_AndNoStackTrace()
    {
        using var services = BuildServices();
        var context = NewContext(services, "req-1");

        await Pipeline(services, _ => throw new InvalidOperationException("secret internals"))(context);

        Assert.Equal(500, context.Response.StatusCode);
        var json = ReadJson(context);
        Assert.Equal(500, json.GetProperty("status").GetInt32());
        Assert.Equal("Internal Server Error", json.GetProperty("title").GetString());
        Assert.Equal("req-1", json.GetProperty("correlationId").GetString());
        Assert.DoesNotContain("secret internals", json.GetProperty("detail").GetString());
        Assert.Equal("req-1", context.Response.Headers[CorrelationIdMiddleware.HeaderName]);   // header survives the error response
    }

    [Fact]
    public async Task UnhandledException_IsLoggedAsErrorWithTheCorrelationId_AndTheRequestLineShows500()
    {
        using var services = BuildServices();
        var context = NewContext(services, "req-2");
        context.Request.Method = "POST";
        context.Request.Path = "/api/orders";

        await Pipeline(services, _ => throw new InvalidOperationException("boom"))(context);

        // The error is written by the middleware that sits OUTSIDE the correlation middleware: it must still carry the id.
        var error = Assert.Single(_logs.Entries, e => e.Level == LogLevel.Error);
        Assert.Contains("CorrelationId:req-2", error.Scopes);
        // The request line is written before the exception middleware sets the status, so it reports 500 explicitly.
        var request = Assert.Single(_logs.Entries, e => e.Level == LogLevel.Information);
        Assert.Contains("POST /api/orders responded 500", request.Message);
        Assert.Contains("CorrelationId:req-2", request.Scopes);
    }

    [Fact]
    public async Task UnhandledException_InDevelopment_IncludesDetails()
    {
        using var services = BuildServices("Development");
        var context = NewContext(services);

        await Pipeline(services, _ => throw new InvalidOperationException("boom"))(context);

        Assert.Contains("boom", ReadJson(context).GetProperty("detail").GetString());
    }

    [Fact]
    public async Task ClientCancellation_IsNotAServerError_AndIsLoggedAs499()
    {
        using var services = BuildServices();
        using var cts = new CancellationTokenSource();
        var context = NewContext(services, "req-3");
        context.RequestAborted = cts.Token;
        context.Request.Method = "GET";
        context.Request.Path = "/api/events";

        await Pipeline(services, _ => { cts.Cancel(); throw new OperationCanceledException(cts.Token); })(context);

        Assert.Equal(499, context.Response.StatusCode);
        Assert.DoesNotContain(_logs.Entries, e => e.Level >= LogLevel.Error);
        Assert.Contains(_logs.Entries, e => e.Message.Contains("GET /api/events responded 499"));
    }

    private sealed class FakeEnvironment : IHostEnvironment
    {
        public FakeEnvironment(string name) => EnvironmentName = name;
        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; } = "Tickify.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
