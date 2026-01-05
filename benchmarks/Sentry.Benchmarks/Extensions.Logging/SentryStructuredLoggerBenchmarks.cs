#nullable enable

using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging;
using Sentry.Extensibility;
using Sentry.Extensions.Logging;
using Sentry.Internal;
using Sentry.Testing;

namespace Sentry.Benchmarks.Extensions.Logging;

public class SentryStructuredLoggerBenchmarks
{
    private Hub _hub = null!;
    private Sentry.Extensions.Logging.SentryStructuredLogger _logger = null!;
    private LogRecord _logRecord = null!;
    private SentryLog? _lastLog;

    [GlobalSetup]
    public void Setup()
    {
        SentryLoggingOptions options = new()
        {
            Dsn = DsnSamples.ValidDsn,
            EnableLogs = true,
        };
        options.SetBeforeSendLog((SentryLog log) =>
        {
            _lastLog = log;
            return null;
        });

        MockClock clock = new(new DateTimeOffset(2025, 04, 22, 14, 51, 00, 789, TimeSpan.FromHours(2)));
        SdkVersion sdk = new()
        {
            Name = "SDK Name",
            Version = "SDK Version",
        };

        _hub = new Hub(options, DisabledHub.Instance);
        _logger = new Sentry.Extensions.Logging.SentryStructuredLogger("CategoryName", options, _hub, clock, sdk);
        _logRecord = new LogRecord(LogLevel.Information, new EventId(2025, "EventName"), new InvalidOperationException("exception-message"), "Number={Number}, Text={Text}", 2018, "message");
    }

    [Benchmark]
    public void Log()
    {
        _logger.Log(_logRecord.LogLevel, _logRecord.EventId, _logRecord.Exception, _logRecord.Message, _logRecord.Args);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _hub.Dispose();

        if (_lastLog is null)
        {
            throw new InvalidOperationException("Last Log is null");
        }
        if (_lastLog.Message != "Number=2018, Text=message")
        {
            throw new InvalidOperationException($"Last Log with Message: '{_lastLog.Message}'");
        }
    }

    private sealed class LogRecord
    {
        public LogRecord(LogLevel logLevel, EventId eventId, Exception? exception, string? message, params object?[] args)
        {
            LogLevel = logLevel;
            EventId = eventId;
            Exception = exception;
            Message = message;
            Args = args;
        }

        public LogLevel LogLevel { get; }
        public EventId EventId { get; }
        public Exception? Exception { get; }
        public string? Message { get; }
        public object?[] Args { get; }
    }
}
