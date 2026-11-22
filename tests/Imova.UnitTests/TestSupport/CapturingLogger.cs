using Microsoft.Extensions.Logging;

namespace Imova.UnitTests.TestSupport;

// Records every log line with its level, category, rendered message and the scope values open at
// the time (where business events keep their ids — see AppEvents).
public sealed class CapturingLoggerFactory : ILoggerFactory
{
    public sealed record Entry(string Category, LogLevel Level, string Message, IReadOnlyDictionary<string, object?> Scope);

    private readonly List<Entry> _entries = [];
    private readonly AsyncLocal<List<IReadOnlyList<KeyValuePair<string, object?>>>?> _scopes = new();

    public IReadOnlyList<Entry> Entries => _entries;

    public ILogger CreateLogger(string categoryName) => new Logger(this, categoryName);

    public void AddProvider(ILoggerProvider provider) { }

    public void Dispose() { }

    private sealed class Logger(CapturingLoggerFactory factory, string category) : ILogger
    {
        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            var stack = factory._scopes.Value ??= [];
            var values = state is IEnumerable<KeyValuePair<string, object>> pairs
                ? pairs.Select(p => new KeyValuePair<string, object?>(p.Key, p.Value)).ToList()
                : [];
            stack.Add(values);
            return new Pop(stack);
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var scope = new Dictionary<string, object?>();
            foreach (var pair in (factory._scopes.Value ?? []).SelectMany(s => s))
            {
                scope[pair.Key] = pair.Value;
            }

            factory._entries.Add(new Entry(category, logLevel, formatter(state, exception), scope));
        }
    }

    private sealed class Pop(List<IReadOnlyList<KeyValuePair<string, object?>>> stack) : IDisposable
    {
        public void Dispose() => stack.RemoveAt(stack.Count - 1);
    }
}
