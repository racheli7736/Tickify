using Microsoft.Extensions.Logging;

namespace Tickify.Tests.Helpers;

/// <summary>A logging provider that records every entry together with the scopes that were open when it was written.</summary>
public sealed class CapturingLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    public sealed record Entry(LogLevel Level, string Category, string Message, IReadOnlyList<string> Scopes);

    private IExternalScopeProvider _scopes = new LoggerExternalScopeProvider();
    private readonly List<Entry> _entries = new();

    public IReadOnlyList<Entry> Entries => _entries;

    public ILoggerFactory CreateFactory() => LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Trace).AddProvider(this));

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(this, categoryName);

    public void SetScopeProvider(IExternalScopeProvider scopeProvider) => _scopes = scopeProvider;

    public void Dispose() { }

    private sealed class CapturingLogger : ILogger
    {
        private readonly CapturingLoggerProvider _provider;
        private readonly string _category;

        public CapturingLogger(CapturingLoggerProvider provider, string category)
        {
            _provider = provider;
            _category = category;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => _provider._scopes.Push(state);

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var scopes = new List<string>();
            _provider._scopes.ForEachScope((scope, list) => list.Add(scope?.ToString() ?? string.Empty), scopes);
            _provider._entries.Add(new Entry(logLevel, _category, formatter(state, exception), scopes));
        }
    }
}
