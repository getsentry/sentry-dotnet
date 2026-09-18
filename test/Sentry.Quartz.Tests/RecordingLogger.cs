using Microsoft.Extensions.Logging;

namespace Sentry.Quartz.Tests;

/// <summary>
/// A minimal <see cref="ILogger{TCategoryName}"/> that records every log entry so tests can assert on the
/// messages produced by the source-generated <c>LoggerMessage</c> methods in <see cref="SentryCronJobMiddleware"/>.
/// </summary>
internal sealed class RecordingLogger<T> : ILogger<T>
{
    public List<(LogLevel Level, EventId EventId, string Message, Exception? Exception)> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Entries.Add((logLevel, eventId, formatter(state, exception), exception));
    }
}
