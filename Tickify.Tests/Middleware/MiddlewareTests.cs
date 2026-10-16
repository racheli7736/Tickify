using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tickify.API.Extensions;
using Tickify.API.Middleware;

namespace Tickify.Tests.Middleware;

public class MiddlewareTests
{
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

    private static CorrelationIdMiddleware Correlation(RequestDelegate next) =>
        new(next, NullLogger<CorrelationIdMiddleware>.Instance);

    private static ExceptionHandlingMiddleware ExceptionHandling(ServiceProvider services, RequestDelegate next) =>
        new(next, NullLogger<ExceptionHandlingMiddleware>.Instance,
            services.GetRequiredService<IProblemDetailsService>(), services.GetRequiredService<IHostEnvironment>());

    private static JsonElement ReadJson(HttpContext context)
    {
        context.Response.Body.Position = 0;
        return JsonDocument.Parse(context.Response.Body).RootElement;
    }

    [Fact]
    public async Task CorrelationId_UsesIncomingHeader_AndEchoesItInTheResponse()
    {
        using var services = BuildServices();
        var context = NewContext(services, "abc-123");

        await Correlation(_ => Task.CompletedTask).InvokeAsync(context);

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

        await Correlation(_ => Task.CompletedTask).InvokeAsync(context);

        Assert.True(Guid.TryParse(context.GetCorrelationId(), out _));
        Assert.Equal(context.GetCorrelationId(), context.Response.Headers[CorrelationIdMiddleware.HeaderName]);
    }

    [Fact]
    public async Task UnhandledException_Returns500WithUniformJsonAndCorrelationId_AndNoStackTrace()
    {
        using var services = BuildServices();
        var context = NewContext(services, "req-1");
        RequestDelegate pipeline = ctx => ExceptionHandling(services, _ => throw new InvalidOperationException("secret internals")).InvokeAsync(ctx);

        await Correlation(pipeline).InvokeAsync(context);

        Assert.Equal(500, context.Response.StatusCode);
        var json = ReadJson(context);
        Assert.Equal(500, json.GetProperty("status").GetInt32());
        Assert.Equal("Internal Server Error", json.GetProperty("title").GetString());
        Assert.Equal("req-1", json.GetProperty("correlationId").GetString());
        Assert.DoesNotContain("secret internals", json.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task UnhandledException_InDevelopment_IncludesDetails()
    {
        using var services = BuildServices("Development");
        var context = NewContext(services);

        await ExceptionHandling(services, _ => throw new InvalidOperationException("boom")).InvokeAsync(context);

        Assert.Contains("boom", ReadJson(context).GetProperty("detail").GetString());
    }

    [Fact]
    public async Task ClientCancellation_IsNotReportedAsServerError()
    {
        using var services = BuildServices();
        using var cts = new CancellationTokenSource();
        var context = NewContext(services);
        context.RequestAborted = cts.Token;
        var logger = new CapturingLogger();
        var middleware = new ExceptionHandlingMiddleware(
            _ => { cts.Cancel(); throw new OperationCanceledException(cts.Token); },
            logger, services.GetRequiredService<IProblemDetailsService>(), services.GetRequiredService<IHostEnvironment>());

        await middleware.InvokeAsync(context);

        Assert.Equal(499, context.Response.StatusCode);
        Assert.DoesNotContain(logger.Levels, l => l >= LogLevel.Error);
    }

    private sealed class FakeEnvironment : IHostEnvironment
    {
        public FakeEnvironment(string name) => EnvironmentName = name;
        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; } = "Tickify.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }

    private sealed class CapturingLogger : ILogger<ExceptionHandlingMiddleware>
    {
        public List<LogLevel> Levels { get; } = new();
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Levels.Add(logLevel);
    }
}
