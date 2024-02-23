using Sentry.Internal.Http;
using BackgroundWorker = Sentry.Internal.BackgroundWorker;

namespace Sentry.Tests.Internals;

public class BackgroundWorkerTests
{
    private readonly Fixture _fixture;

    public BackgroundWorkerTests(ITestOutputHelper outputHelper)
    {
        _fixture = new Fixture(outputHelper);
    }

    private class Fixture
    {
        public IClientReportRecorder ClientReportRecorder { get; private set; } = Substitute.For<IClientReportRecorder>();
        public ITransport Transport { get; set; } = Substitute.For<ITransport>();
        public IDiagnosticLogger Logger { get; set; }
        public ConcurrentQueue<Envelope> Queue { get; set; } = new();
        public CancellationTokenSource CancellationTokenSource { get; set; } = new();
        public SentryOptions SentryOptions { get; set; } = new();

        private readonly TimeSpan _defaultShutdownTimeout;

        public Fixture(ITestOutputHelper outputHelper)
        {
            // Use the test output logger, but spy on it so we can check received calls.
            // See "Test spies" at https://nsubstitute.github.io/help/partial-subs/
            Logger = Substitute.ForPartsOf<TestOutputDiagnosticLogger>(outputHelper);
            // Logger = Substitute.For<IDiagnosticLogger>();
            // Logger.IsEnabled(Arg.Any<SentryLevel>()).Returns(true);

            // Make sure we always return a task from the substitute transport to prevent flaky tests.
            Transport.SendEnvelopeAsync(Arg.Any<Envelope>(), Arg.Any<CancellationToken>())
                .Returns(callInfo =>
                {
                    var token = callInfo.Arg<CancellationToken>();
                    return token.IsCancellationRequested ? Task.FromCanceled(token) : Task.CompletedTask;
                });

            SentryOptions.Dsn = ValidDsn;
            SentryOptions.Debug = true;
            SentryOptions.DiagnosticLogger = Logger;
            SentryOptions.ClientReportRecorder = ClientReportRecorder;

            // For most of these tests, we don't want to rely on shutdown timeout
            _defaultShutdownTimeout = SentryOptions.ShutdownTimeout;
            SentryOptions.ShutdownTimeout = TimeSpan.Zero;
        }

        public BackgroundWorker GetSut()
            => new(
                Transport,
                SentryOptions,
                shutdownSource: CancellationTokenSource,
                queue: Queue);

        public void UseDefaultShutdownTimeout()
        {
            SentryOptions.ShutdownTimeout = _defaultShutdownTimeout;
        }

        public IClientReportRecorder UseRealClientReportRecorder()
        {
            ClientReportRecorder = new ClientReportRecorder(SentryOptions);
            SentryOptions.ClientReportRecorder = ClientReportRecorder;
            return ClientReportRecorder;
        }
    }

    [Fact]
    public void Ctor_Task_Created()
    {
        using var sut = _fixture.GetSut();
        Assert.NotNull(sut.WorkerTask);
    }

    [Fact]
    public void Dispose_StopsTask()
    {
        var sut = _fixture.GetSut();
        sut.Dispose();

        Assert.Equal(TaskStatus.RanToCompletion, sut.WorkerTask.Status);
    }

    [Fact]
    public async Task Dispose_WhenRequestInFlight_StopsTask()
    {
        var tcs = new TaskCompletionSource<object>();
        var envelope = Envelope.FromEvent(new SentryEvent());

        _fixture.Transport
            .When(t => t.SendEnvelopeAsync(envelope, Arg.Any<CancellationToken>()))
            .Do(_ => tcs.SetResult(null));

        var sut = _fixture.GetSut();
        sut.EnqueueEnvelope(envelope);

        await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(3)));
        Assert.True(tcs.Task.IsCompleted);

        sut.Dispose();

        Assert.Equal(TaskStatus.RanToCompletion, sut.WorkerTask.Status);
    }

    [Fact]
    public void Dispose_TokenCancelledWhenRequestInFlight_StopsTask()
    {
        var envelope = Envelope.FromEvent(new SentryEvent());

        _fixture.Transport
            .SendEnvelopeAsync(envelope, Arg.Any<CancellationToken>())
            .Throws(new OperationCanceledException());

        var sut = _fixture.GetSut();
        sut.EnqueueEnvelope(envelope);

        sut.Dispose();

        Assert.Equal(TaskStatus.RanToCompletion, sut.WorkerTask.Status);
    }

    [Fact]
    public async Task Dispose_SwallowsException()
    {
        _fixture.CancellationTokenSource.Dispose();
        var sut = _fixture.GetSut();

        // We expect an exception here, because we disposed the cancellation token source
        await Assert.ThrowsAsync<ObjectDisposedException>(() => sut.WorkerTask);

        // No exception should be thrown here
        sut.Dispose();

        Assert.Equal(TaskStatus.Faulted, sut.WorkerTask.Status);
    }

    [Fact]
    public void Dispose_EventQueuedZeroShutdownTimeout_CantEmptyQueueBeforeShutdown()
    {
        _fixture.SentryOptions.ShutdownTimeout = TimeSpan.Zero;

        // Start the worker and enqueue a few items
        var sut = _fixture.GetSut();
        for (var i = 0; i < 3; i++)
        {
            sut.EnqueueEnvelope(Envelope.FromEvent(new SentryEvent()), process: false);
        }

        // Disposing the worker should stop its internal task
        sut.Dispose();

        // The worker task should have stopped
        Assert.Equal(TaskStatus.RanToCompletion, sut.WorkerTask.Status);

        // Worker was stopped before queue could be emptied.
        Assert.NotEmpty(_fixture.Queue);
    }

    [Fact]
    public void Dispose_EventQueuedDefaultShutdownTimeout_EmptiesQueueBeforeShutdown()
    {
        _fixture.UseDefaultShutdownTimeout();

        // Start the worker and enqueue a few items
        var sut = _fixture.GetSut();
        for (var i = 0; i < 3; i++)
        {
            sut.EnqueueEnvelope(Envelope.FromEvent(new SentryEvent()), process: false);
        }

        // Disposing the worker should stop its internal task
        // Time this operation for comparison later
        var sw = Stopwatch.StartNew();
        sut.Dispose();
        sw.Stop();

        // The worker task should have stopped
        Assert.Equal(TaskStatus.RanToCompletion, sut.WorkerTask.Status);

        // Worker was given time to empty before it was stopped.
        Assert.Empty(_fixture.Queue);

        // We should not have used the entire shutdown timeout period.
        sw.Elapsed.Should().BeLessThan(_fixture.SentryOptions.ShutdownTimeout,
            "The worker should have stopped before the timeout expired.");
    }

    [Fact]
    public async Task Create_CancelledTaskAndNoShutdownTimeout_ConsumesNoEvents()
    {
        // Arrange
        _fixture.SentryOptions.ShutdownTimeout = TimeSpan.Zero;
        _fixture.CancellationTokenSource.Cancel();

        // Act
        using var sut = _fixture.GetSut();

        // Wait 3 seconds for task to finish
        await Task.WhenAny(sut.WorkerTask, Task.Delay(TimeSpan.FromSeconds(3)));

        // Assert
        Assert.Equal(TaskStatus.RanToCompletion, sut.WorkerTask.Status);
    }

    [Fact]
    public void CaptureEvent_LimitReached_EventDropped()
    {
        // Arrange
        var envelope = Envelope.FromEvent(new SentryEvent());
        _fixture.SentryOptions.MaxQueueItems = 1;

        using var sut = _fixture.GetSut();
        sut.EnqueueEnvelope(envelope, process: false);

        // Act
        var queued = sut.EnqueueEnvelope(envelope);

        // Assert
        Assert.False(queued); // Fails to queue second
    }

    [Fact]
    public void CaptureEvent_LimitReached_RecordsDiscardedEvent()
    {
        // Arrange
        var envelope = Envelope.FromEvent(new SentryEvent());
        _fixture.SentryOptions.MaxQueueItems = 1;

        using var sut = _fixture.GetSut();
        sut.EnqueueEnvelope(envelope, process: false);

        // Act
        sut.EnqueueEnvelope(envelope);

        // Assert

        // Check that we counted a single discarded event with the correct information
        _fixture.ClientReportRecorder.Received(1)
            .RecordDiscardedEvent(DiscardReason.QueueOverflow, DataCategory.Error);
    }

    [Fact]
    public void CaptureEvent_DisposedWorker_ThrowsObjectDisposedException()
    {
        // Arrange
        var envelope = Envelope.FromEvent(new SentryEvent());

        // Act
        var sut = _fixture.GetSut();
        sut.Dispose();

        // Assert
        Assert.Throws<ObjectDisposedException>(() => sut.EnqueueEnvelope(envelope));
    }

    [Fact]
    public void CaptureEvent_InnerTransportInvoked()
    {
        // Arrange
        var envelope = Envelope.FromEvent(new SentryEvent());
        _fixture.UseDefaultShutdownTimeout();
        var sut = _fixture.GetSut();

        // Act
        var queued = sut.EnqueueEnvelope(envelope);
        sut.Dispose();

        // Assert
        Assert.True(queued);
        _fixture.Transport.Received(1).SendEnvelopeAsync(envelope, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void CaptureEvent_InnerTransportThrows_WorkerSuppresses()
    {
        // Arrange
        var envelope = Envelope.FromEvent(new SentryEvent());

        _fixture.Transport
            .When(e => e.SendEnvelopeAsync(envelope))
            .Do(_ => throw new Exception("Sending to sentry failed."));

        using var sut = _fixture.GetSut();
        // Act
        var queued = sut.EnqueueEnvelope(envelope);

        // Assert
        Assert.True(queued);

        // TODO: tap into exception handling (some ILogger or Action<Exception>)
    }

    [Fact]
    public void QueuedItems_StartsEmpty()
    {
        using var sut = _fixture.GetSut();
        Assert.Equal(0, sut.QueuedItems);
    }

    [Fact]
    public void QueuedItems_ReflectsQueue()
    {
        _fixture.Queue.Enqueue(null);
        using var sut = _fixture.GetSut();
        Assert.Equal(1, sut.QueuedItems);
    }

    [Fact]
    public async Task FlushAsync_DisposedWorker_LogsAndReturns()
    {
        var sut = _fixture.GetSut();
        sut.Dispose();
        await sut.FlushAsync(TimeSpan.MaxValue);
        _fixture.Logger.Received(1).Log(SentryLevel.Debug, "Worker disposed. Nothing to flush.");
    }

    [Fact]
    public async Task FlushAsync_SingleEvent_FlushReturnsAfterEventSent()
    {
        // Arrange
        var envelope = Envelope.FromEvent(new SentryEvent());
        using var sut = _fixture.GetSut();

        // Act
        sut.EnqueueEnvelope(envelope, process: false);

        var flushTask = sut.FlushAsync(Timeout.InfiniteTimeSpan);
        Assert.Single(_fixture.Queue); // Event being processed

        // Release the item and flush
        sut.ProcessQueuedItems(1);
        await flushTask;

        // Assert
        _fixture.Logger.Received(1).Log(SentryLevel.Debug, "Successfully flushed all events up to call to FlushAsync.");
        Assert.Empty(_fixture.Queue);
    }

    [Fact]
    public async Task FlushAsync_ZeroTimeout_Accepted()
    {
        // Arrange
        var envelope = Envelope.FromEvent(new SentryEvent());
        using var sut = _fixture.GetSut();

        // Act
        sut.EnqueueEnvelope(envelope, process: false);
        await sut.FlushAsync(TimeSpan.Zero);

        // Assert
        _fixture.Logger.Received(1).Log(SentryLevel.Debug, "Timeout or shutdown already requested. Exiting.");
    }

    [Fact]
    public async Task FlushAsync_FullQueue_RespectsTimeout()
    {
        // NOTE: This test is supposed to take at least as long as the timeout
        var flushTimeout = TimeSpan.FromMilliseconds(500);

        // Arrange
        var envelope = Envelope.FromEvent(new SentryEvent());
        _fixture.SentryOptions.MaxQueueItems = 1;
        using var sut = _fixture.GetSut();

        // Act
        sut.EnqueueEnvelope(envelope, process: false);

        var sw = Stopwatch.StartNew();
        await sut.FlushAsync(flushTimeout);
        sw.Stop();

        _fixture.Logger.Received(1).Log(SentryLevel.Debug, "Timeout when trying to flush queue.");
        Assert.Single(_fixture.Queue); // Only the item being processed at the blocked callback

        // Test the timeout
        sw.Elapsed.Should().BeGreaterThan(flushTimeout);
    }

    [Fact]
    public async Task FlushAsync_EmptyQueueWithReport_SendsFinalClientReport()
    {
        // Arrange
        _fixture.UseRealClientReportRecorder()
            .RecordDiscardedEvent(DiscardReason.EventProcessor, DataCategory.Internal);
        using var sut = _fixture.GetSut();

        // Act
        await sut.FlushAsync(TimeSpan.MaxValue);

        // Assert
        _fixture.Logger.Received(1).Log(SentryLevel.Debug, "Sending client report after flushing queue.");
        await _fixture.Transport.Received(1).SendEnvelopeAsync(
            Arg.Is<Envelope>(e => e.Items.Count == 1 && e.Items[0].TryGetType() == "client_report"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FlushAsync_EmptyQueueWithNoReport_DoesntSendFinalClientReport()
    {
        // Arrange
        _fixture.UseRealClientReportRecorder();
        using var sut = _fixture.GetSut();

        // Act
        await sut.FlushAsync(TimeSpan.MaxValue);

        // Assert
        _fixture.Logger.DidNotReceive().Log(SentryLevel.Debug, "Sending client report after flushing queue.");
        await _fixture.Transport.DidNotReceive().SendEnvelopeAsync(Arg.Any<Envelope>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FlushAsync_ItemInQueueWithReport_SendsFinalClientReport()
    {
        // Arrange
        _fixture.UseRealClientReportRecorder()
            .RecordDiscardedEvent(DiscardReason.EventProcessor, DataCategory.Internal);

        using var sut = _fixture.GetSut();
        sut.EnqueueEnvelope(Envelope.FromEvent(new SentryEvent()));

        // Act
        await sut.FlushAsync(TimeSpan.MaxValue);

        // Assert
        _fixture.Logger.Received(1).Log(SentryLevel.Debug, "Sending client report after flushing queue.");
        await _fixture.Transport.Received(1).SendEnvelopeAsync(
            Arg.Is<Envelope>(e => e.Items.Count == 1 && e.Items[0].TryGetType() == "client_report"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FlushAsync_ItemInQueueGetsRateLimited_SendsFinalClientReport()
    {
        // Arrange
        var recorder = _fixture.UseRealClientReportRecorder();
        _fixture.Transport
            .When(t => t.SendEnvelopeAsync(
                Arg.Is<Envelope>(e => e.Items.Any(i => i.TryGetType() == "event")),
                Arg.Any<CancellationToken>()))
            .Do(_ =>
            {
                // Simulate rate limiting for all events
                recorder.RecordDiscardedEvent(DiscardReason.RateLimitBackoff, DataCategory.Error);
            });

        using var sut = _fixture.GetSut();
        sut.EnqueueEnvelope(Envelope.FromEvent(new SentryEvent()));

        // Act
        await sut.FlushAsync(TimeSpan.MaxValue);

        // Assert
        _fixture.Logger.Received(1).Log(SentryLevel.Debug, "Sending client report after flushing queue.");
        await _fixture.Transport.Received(1).SendEnvelopeAsync(
            Arg.Is<Envelope>(e => e.Items.Count == 1 && e.Items[0].TryGetType() == "client_report"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FlushAsync_Calls_CachingTransport_FlushAsync()
    {
        // Arrange
        var fileSystem = new FakeFileSystem();
        using var tempDir = new TempDirectory(fileSystem);

        var options = _fixture.SentryOptions;
        options.FileSystem = fileSystem;
        options.CacheDirectoryPath = tempDir.Path;

        var innerTransport = _fixture.Transport;
        _fixture.Transport = CachingTransport.Create(innerTransport, options, startWorker: false);

        using var sut = _fixture.GetSut();
        var envelope = Envelope.FromEvent(new SentryEvent());

        // Act
        sut.EnqueueEnvelope(envelope, process: false);
        sut.ProcessQueuedItems(1);
        await sut.FlushAsync(Timeout.InfiniteTimeSpan);

        // Assert
        _fixture.Logger.Received(1)
            .Log(SentryLevel.Debug, "CachingTransport received request to flush the cache.");
    }
}
