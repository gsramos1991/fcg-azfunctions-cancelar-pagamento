using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace FCGPagamentos.Functions.Tests
{
    public class LogEntry
    {
        public LogLevel Level { get; set; }
        public EventId EventId { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public Exception? Exception { get; set; }
    }

    public sealed class TestLoggerFactory : ILoggerFactory
    {
        private readonly ConcurrentBag<LogEntry> _entries = new();

        public IEnumerable<LogEntry> Entries => _entries.ToArray();

        public void AddProvider(ILoggerProvider provider) { }

        public ILogger CreateLogger(string categoryName) => new TestLogger(categoryName, _entries);

        public void Dispose() { }
    }

    internal sealed class TestLogger : ILogger
    {
        private readonly string _category;
        private readonly ConcurrentBag<LogEntry> _entries;

        public TestLogger(string category, ConcurrentBag<LogEntry> entries)
        {
            _category = category;
            _entries = entries;
        }

        public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var msg = formatter(state, exception);
            _entries.Add(new LogEntry
            {
                Level = logLevel,
                EventId = eventId,
                Category = _category,
                Message = msg,
                Exception = exception
            });
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }
}

