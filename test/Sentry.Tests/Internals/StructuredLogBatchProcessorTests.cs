#nullable enable

namespace Sentry.Tests.Internals;

public class StructuredLogBatchProcessorTests : IDisposable
{
    private sealed class Fixture
    {
        private readonly IHub _hub;

        public ClientReportRecorder ClientReportRecorder { get; }
        public InMemoryDiagnosticLogger DiagnosticLogger { get; }
        public BlockingCollection<Envelope> CapturedEnvelopes { get; }

        public int ExpectedDiagnosticLogs { get; set; }

        public Fixture()
        {
            var options = new SentryOptions();
            var clock = new MockClock();

            _hub = Substitute.For<IHub>();
            ClientReportRecorder = new ClientReportRecorder(options, clock);
            DiagnosticLogger = new InMemoryDiagnosticLogger();

            CapturedEnvelopes = [];
            _hub.CaptureEnvelope(Arg.Do<Envelope>(arg => CapturedEnvelopes.Add(arg)));
            _hub.IsEnabled.Returns(true);

            ExpectedDiagnosticLogs = 0;
        }

        public void DisableHub()
        {
            _hub.IsEnabled.Returns(false);
        }

        public StructuredLogBatchProcessor GetSut(int batchCount)
        {
            return new StructuredLogBatchProcessor(_hub, batchCount, Timeout.InfiniteTimeSpan, ClientReportRecorder, DiagnosticLogger);
        }
    }

    private readonly Fixture _fixture = new();

    public void Dispose()
    {
        Assert.Equal(_fixture.ExpectedDiagnosticLogs, _fixture.DiagnosticLogger.Entries.Count);
    }

    [Fact]
    public void Enqueue_NeitherSizeNorTimeoutReached_DoesNotCaptureEnvelope()
    {
        using var processor = _fixture.GetSut(2);

        processor.Enqueue(CreateLog("one"));

        Assert.Empty(_fixture.CapturedEnvelopes);
        AssertEnvelope();
    }

    [Fact]
    public void Enqueue_SizeReached_CaptureEnvelope()
    {
        using var processor = _fixture.GetSut(2);

        processor.Enqueue(CreateLog("one"));
        processor.Enqueue(CreateLog("two"));

        Assert.Single(_fixture.CapturedEnvelopes);
        AssertEnvelope("one", "two");
    }

    [Fact]
    public void Enqueue_TimeoutReached_CaptureEnvelope()
    {
        using var processor = _fixture.GetSut(2);

        processor.Enqueue(CreateLog("one"));
        processor.OnIntervalElapsed();

        Assert.Single(_fixture.CapturedEnvelopes);
        AssertEnvelope("one");
    }

    [Fact]
    public void Enqueue_BothSizeAndTimeoutReached_CaptureEnvelopeOnce()
    {
        using var processor = _fixture.GetSut(2);

        processor.Enqueue(CreateLog("one"));
        processor.Enqueue(CreateLog("two"));
        processor.OnIntervalElapsed();

        Assert.Single(_fixture.CapturedEnvelopes);
        AssertEnvelope("one", "two");
    }

    [Fact]
    public void Enqueue_BothTimeoutAndSizeReached_CaptureEnvelopes()
    {
        using var processor = _fixture.GetSut(2);

        processor.OnIntervalElapsed();
        processor.Enqueue(CreateLog("one"));
        processor.OnIntervalElapsed();
        processor.Enqueue(CreateLog("two"));
        processor.Enqueue(CreateLog("three"));

        Assert.Equal(2, _fixture.CapturedEnvelopes.Count);
        AssertEnvelopes(["one"], ["two", "three"]);
    }

    [Fact]
    public async Task Enqueue_Concurrency_CaptureEnvelopes()
    {
        Skip.If(TestEnvironment.IsGitHubActions, "Timeout may exceed on CI");

        const int batchCount = 5;
        const int maxDegreeOfParallelism = 10;
        const int logsPerTask = 1_000;

        using var processor = _fixture.GetSut(batchCount);
        using var sync = new ManualResetEvent(false);

        var tasks = new Task[maxDegreeOfParallelism];
        for (var i = 0; i < tasks.Length; i++)
        {
            tasks[i] = Task.Factory.StartNew(static state =>
            {
                var (sync, logsPerTask, taskIndex, processor) = ((ManualResetEvent, int, int, StructuredLogBatchProcessor))state!;
                sync.WaitOne(5_000);
                for (var i = 0; i < logsPerTask; i++)
                {
                    processor.Enqueue(CreateLog($"{taskIndex}-{i}"));
                }
            }, (sync, logsPerTask, i, processor));
        }

        sync.Set();
        await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(5));
        processor.Flush();
        _fixture.CapturedEnvelopes.CompleteAdding();

        var capturedLogs = _fixture.CapturedEnvelopes
            .SelectMany(static envelope => envelope.Items)
            .Select(static item => item.Payload)
            .OfType<JsonSerializable>()
            .Select(static payload => payload.Source)
            .OfType<StructuredLog>()
            .Sum(log => log.Items.Length);
        var droppedLogs = 0;

        if (_fixture.ClientReportRecorder.GenerateClientReport() is { } clientReport)
        {
            var discardedEvent = Assert.Single(clientReport.DiscardedEvents);
            Assert.Equal(new DiscardReasonWithCategory(DiscardReason.Backpressure, DataCategory.Default), discardedEvent.Key);

            droppedLogs = discardedEvent.Value;
            _fixture.ExpectedDiagnosticLogs = discardedEvent.Value;
        }

        var actualInvocations = maxDegreeOfParallelism * logsPerTask;
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

    [Fact]
    public void Enqueue_HubDisabled_DoesNotCaptureEnvelope()
    {
        var processor = _fixture.GetSut(2);

        processor.Enqueue(CreateLog("one"));
        _fixture.DisableHub();
        processor.Enqueue(CreateLog("two"));
        Assert.Empty(_fixture.CapturedEnvelopes);

        processor.Flush();
        Assert.Single(_fixture.CapturedEnvelopes);
        AssertEnvelope("one");
    }

    [Fact]
    public void Flush_NeitherSizeNorTimeoutReached_CaptureEnvelope()
    {
        using var processor = _fixture.GetSut(2);

        processor.Enqueue(CreateLog("one"));
        processor.Flush();

        Assert.Single(_fixture.CapturedEnvelopes);
        AssertEnvelope("one");
    }

    [Fact]
    public void Dispose_Enqueue_DoesNotCaptureEnvelope()
    {
        var processor = _fixture.GetSut(2);

        processor.Enqueue(CreateLog("one"));
        processor.Dispose();
        processor.Enqueue(CreateLog("two"));
        processor.Flush();

        Assert.Empty(_fixture.CapturedEnvelopes);
        AssertEnvelope();
        _fixture.ExpectedDiagnosticLogs = 1;
    }

    private static SentryLog CreateLog(string message)
    {
        return new SentryLog(DateTimeOffset.MinValue, SentryId.Empty, SentryLogLevel.Trace, message);
    }

    private void AssertEnvelope(params string[] expected)
    {
        if (expected.Length == 0)
        {
            Assert.Empty(_fixture.CapturedEnvelopes);
            return;
        }

        var envelope = Assert.Single(_fixture.CapturedEnvelopes);
        AssertEnvelope(envelope, expected);
    }

    private void AssertEnvelopes(params string[][] expected)
    {
        if (expected.Length == 0)
        {
            Assert.Empty(_fixture.CapturedEnvelopes);
            return;
        }

        Assert.Equal(expected.Length, _fixture.CapturedEnvelopes.Count);
        var index = 0;
        foreach (var capturedEnvelope in _fixture.CapturedEnvelopes)
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
}
