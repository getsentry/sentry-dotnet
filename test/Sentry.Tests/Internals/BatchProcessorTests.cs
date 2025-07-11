#nullable enable

namespace Sentry.Tests.Internals;

public class BatchProcessorTests : IDisposable
{
    private readonly IHub _hub;
    private readonly MockClock _clock;
    private readonly ClientReportRecorder _clientReportRecorder;
    private readonly InMemoryDiagnosticLogger _diagnosticLogger;
    private readonly BlockingCollection<Envelope> _capturedEnvelopes;

    public BatchProcessorTests()
    {
        var options = new SentryOptions();

        _hub = Substitute.For<IHub>();
        _clock = new MockClock();
        _clientReportRecorder = new ClientReportRecorder(options, _clock);
        _diagnosticLogger = new InMemoryDiagnosticLogger();

        _capturedEnvelopes = [];
        _hub.CaptureEnvelope(Arg.Do<Envelope>(arg => _capturedEnvelopes.Add(arg)));
    }

    [Theory(Skip = "May no longer be required after feedback.")]
    [InlineData(-1)]
    [InlineData(0)]
    public void Ctor_CountOutOfRange_Throws(int count)
    {
        var ctor = () => new BatchProcessor(_hub, count, TimeSpan.FromMilliseconds(10), _clock, _clientReportRecorder, _diagnosticLogger);

        Assert.Throws<ArgumentOutOfRangeException>(ctor);
    }

    [Theory(Skip = "May no longer be required after feedback.")]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(int.MaxValue + 1.0)]
    public void Ctor_IntervalOutOfRange_Throws(double interval)
    {
        var ctor = () => new BatchProcessor(_hub, 1, TimeSpan.FromMilliseconds(interval), _clock, _clientReportRecorder, _diagnosticLogger);

        Assert.Throws<ArgumentException>(ctor);
    }

    [Fact]
    public void Enqueue_NeitherSizeNorTimeoutReached_DoesNotCaptureEnvelope()
    {
        using var processor = new BatchProcessor(_hub, 2, Timeout.InfiniteTimeSpan, _clock, _clientReportRecorder, _diagnosticLogger);

        processor.Enqueue(CreateLog("one"));

        Assert.Empty(_capturedEnvelopes);
        AssertEnvelope();
    }

    [Fact]
    public void Enqueue_SizeReached_CaptureEnvelope()
    {
        using var processor = new BatchProcessor(_hub, 2, Timeout.InfiniteTimeSpan, _clock, _clientReportRecorder, _diagnosticLogger);

        processor.Enqueue(CreateLog("one"));
        processor.Enqueue(CreateLog("two"));

        Assert.Single(_capturedEnvelopes);
        AssertEnvelope("one", "two");
    }

    [Fact]
    public void Enqueue_TimeoutReached_CaptureEnvelope()
    {
        using var processor = new BatchProcessor(_hub, 2, Timeout.InfiniteTimeSpan, _clock, _clientReportRecorder, _diagnosticLogger);

        processor.Enqueue(CreateLog("one"));

        processor.OnIntervalElapsed(null);

        Assert.Single(_capturedEnvelopes);
        AssertEnvelope("one");
    }

    [Fact]
    public void Enqueue_BothSizeAndTimeoutReached_CaptureEnvelopeOnce()
    {
        using var processor = new BatchProcessor(_hub, 2, Timeout.InfiniteTimeSpan, _clock, _clientReportRecorder, _diagnosticLogger);

        processor.Enqueue(CreateLog("one"));
        processor.Enqueue(CreateLog("two"));
        processor.OnIntervalElapsed(null);

        Assert.Single(_capturedEnvelopes);
        AssertEnvelope("one", "two");
    }

    [Fact]
    public void Enqueue_BothTimeoutAndSizeReached_CaptureEnvelopes()
    {
        using var processor = new BatchProcessor(_hub, 2, Timeout.InfiniteTimeSpan, _clock, _clientReportRecorder, _diagnosticLogger);

        processor.OnIntervalElapsed(null);
        processor.Enqueue(CreateLog("one"));
        processor.OnIntervalElapsed(null);
        processor.Enqueue(CreateLog("two"));
        processor.Enqueue(CreateLog("three"));

        Assert.Equal(2, _capturedEnvelopes.Count);
        AssertEnvelopes(["one"], ["two", "three"]);
    }

    [Fact(Skip = "TODO")]
    public async Task Enqueue_Concurrency_CaptureEnvelopes()
    {
        const int batchCount = 3;
        const int logsPerTask = 100;

        using var processor = new BatchProcessor(_hub, batchCount, Timeout.InfiniteTimeSpan, _clock, _clientReportRecorder, _diagnosticLogger);
        using var sync = new ManualResetEvent(false);

        var tasks = new Task[5];
        for (var i = 0; i < tasks.Length; i++)
        {
            tasks[i] = Task.Factory.StartNew(static state =>
            {
                var (sync, logsPerTask, taskIndex, processor) = ((ManualResetEvent, int, int, BatchProcessor))state!;
                sync.WaitOne(5_000);
                for (var i = 0; i < logsPerTask; i++)
                {
                    processor.Enqueue(CreateLog($"{taskIndex}-{i}"));
                }
            }, (sync, logsPerTask, i, processor));
        }

        sync.Set();
        await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(5));
        _capturedEnvelopes.CompleteAdding();

        var capturedLogs = _capturedEnvelopes
            .SelectMany(static envelope => envelope.Items)
            .Select(static item => item.Payload)
            .OfType<JsonSerializable>()
            .Select(static payload => payload.Source)
            .OfType<StructuredLog>()
            .Sum(log => log.Items.Length);
        var droppedLogs = 0;

        if (_clientReportRecorder.GenerateClientReport() is { } clientReport)
        {
            var discardedEvent = Assert.Single(clientReport.DiscardedEvents);
            Assert.Equal(new DiscardReasonWithCategory(DiscardReason.Backpressure, DataCategory.Default), discardedEvent.Key);

            Assert.Equal(_diagnosticLogger.Entries.Count, discardedEvent.Value);
            droppedLogs = discardedEvent.Value;
            _diagnosticLogger.Entries.Clear();
        }

        var actualInvocations = tasks.Length * logsPerTask;
        if (actualInvocations != capturedLogs + droppedLogs)
        {
            Assert.Fail($"""
                Expected {actualInvocations} combined logs,
                but actually received a total of {capturedLogs + droppedLogs} logs,
                with {capturedLogs} captured logs and {droppedLogs} dropped logs,
                which is a difference of {actualInvocations - capturedLogs - droppedLogs} logs.
                """);
        }
    }

    private static SentryLog CreateLog(string message)
    {
        return new SentryLog(DateTimeOffset.MinValue, SentryId.Empty, SentryLogLevel.Trace, message);
    }

    private void AssertEnvelope(params string[] expected)
    {
        if (expected.Length == 0)
        {
            Assert.Empty(_capturedEnvelopes);
            return;
        }

        var envelope = Assert.Single(_capturedEnvelopes);
        AssertEnvelope(envelope, expected);
    }

    private void AssertEnvelopes(params string[][] expected)
    {
        if (expected.Length == 0)
        {
            Assert.Empty(_capturedEnvelopes);
            return;
        }

        Assert.Equal(expected.Length, _capturedEnvelopes.Count);
        var index = 0;
        foreach (var capturedEnvelope in _capturedEnvelopes)
        {
            AssertEnvelope(capturedEnvelope, expected[index]);
            index++;
        }
    }

    private static void AssertEnvelope(Envelope envelope, string[] expected)
    {
        var item = Assert.Single(envelope.Items);
        var payload = Assert.IsType<JsonSerializable>(item.Payload);
        var log = payload.Source as StructuredLog;
        Assert.NotNull(log);
        Assert.Equal(expected, log.Items.ToArray().Select(static item => item.Message));
    }

    public void Dispose()
    {
        Assert.Empty(_diagnosticLogger.Entries);
    }
}
