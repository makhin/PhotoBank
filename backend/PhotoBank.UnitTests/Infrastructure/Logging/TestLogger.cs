using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace PhotoBank.UnitTests.Infrastructure.Logging;

internal sealed class TestLogger<T> : ILogger<T>
{
    public List<LogEntry> Entries { get; } = new();

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Entries.Add(new LogEntry(logLevel, formatter(state, exception), exception));
    }
}

internal sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);

internal sealed class NullScope : IDisposable
{
    public static readonly NullScope Instance = new();

    public void Dispose()
    {
    }
}
