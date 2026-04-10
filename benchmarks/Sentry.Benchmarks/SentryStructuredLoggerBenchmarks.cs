#nullable enable

using BenchmarkDotNet.Attributes;
using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Testing;

namespace Sentry.Benchmarks;

public class SentryStructuredLoggerBenchmarks
{
    private Hub _hub = null!;
    private SentryStructuredLogger _logger = null!;

    private SentryLog? _lastLog;

    [GlobalSetup]
    public void Setup()
    {
        SentryOptions options = new()
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

        _hub = new Hub(options, DisabledHub.Instance);
        _logger = SentryStructuredLogger.Create(_hub, options, clock);
    }

    [Benchmark]
    public void LogWithoutParameters()
    {
        _logger.LogInfo("Message Text");
    }

    [Benchmark]
    public void LogWithParameters()
    {
        _logger.LogInfo("Template string with arguments: {0}, {1}, {2}, {3}", "string", true, 1, 2.2);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        (_logger as IDisposable)?.Dispose();
        _hub.Dispose();

        if (_lastLog is null)
        {
            throw new InvalidOperationException("Last Log is null");
        }
    }
}
