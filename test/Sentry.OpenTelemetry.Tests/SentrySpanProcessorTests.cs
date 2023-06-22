namespace Sentry.OpenTelemetry.Tests;

public class SentrySpanProcessorTests : IDisposable
{
    private static readonly ActivitySource Tracer = new("SentrySpanProcessorTests", "1.0.0");
    private readonly ActivityListener _listener;

    public SentrySpanProcessorTests()
    {
        // Without a listener, activity source will not create activities
        _listener = new ActivityListener
        {
            ActivityStarted = _ => { },
            ActivityStopped = _ => { },
            ShouldListenTo = _ => true,
            SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllDataAndRecorded,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(_listener);
    }

    public void Dispose()
    {
        _listener?.Dispose();
    }

    private class Fixture
    {
        public SentryOptions Options { get; }

        public ISentryClient Client { get; }

        public ISessionManager SessionManager { get; set; }

        public IInternalScopeManager ScopeManager { get; set; }

        public ISystemClock Clock { get; set; }

        public Fixture()
        {
            Options = new SentryOptions
            {
                Dsn = ValidDsn,
                EnableTracing = true,
                AutoSessionTracking = false
            };

            Client = Substitute.For<ISentryClient>();
        }

        public Hub Hub { get; private set; }

        public Hub GetHub() => Hub ??= new Hub(Options, Client, SessionManager, Clock, ScopeManager);

        public SentrySpanProcessor GetSut()
        {
            return new SentrySpanProcessor(GetHub());
        }
    }

    private readonly Fixture _fixture = new();

    [Fact]
    public void Ctor_OpenTelemetryInstrumenterOption_DoesNotThrowException()
    {
        // Arrange
        _fixture.Options.Instrumenter = Instrumenter.OpenTelemetry;

        // Act
        var sut = _fixture.GetSut();

        // Assert
        Assert.NotNull(sut);
    }

    [Fact]
    public void Ctor_NotOpenTelemetryInstrumenterOption_ThrowsInvalidOperationException()
    {
        // Arrange
        _fixture.Options.Instrumenter = Instrumenter.Sentry;

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _fixture.GetSut());
    }

    [Fact]
    public void Ctor_AddsTransactionProcessorToScope()
    {
        // Arrange
        _fixture.Options.Instrumenter = Instrumenter.OpenTelemetry;

        // Act
        var processor = _fixture.GetSut();

        // Assert
        var scope = _fixture.Hub.ScopeManager.GetCurrent().Key;
        scope.TransactionProcessors.Should().NotBeEmpty();
    }

    [Fact]
    public void TransactionProcessor_WithActivity_SetsTraceAndSpanIds()
    {
        // Arrange
        using var activity = Tracer.StartActivity("Parent");
        var transaction = new Transaction("name", "operation");

        // Act
        var processedTransaction = SentrySpanProcessor.TransactionProcessor(transaction);

        // Assert
        processedTransaction.Contexts.Trace.TraceId.Should().Be(activity?.TraceId.AsSentryId());
        processedTransaction.Contexts.Trace.SpanId.Should().Be(activity?.SpanId.AsSentrySpanId());
    }

    [Fact]
    public void TransactionProcessor_WithoutActivity_DoesNotModifyTransaction()
    {
        // Arrange
        var transaction = new Transaction("name", "operation");
        var previousTraceId = transaction.Contexts.Trace.TraceId;
        var previousSpanId = transaction.Contexts.Trace.SpanId;

        // Act
        var processedTransaction = SentrySpanProcessor.TransactionProcessor(transaction);

        // Assert
        Assert.Equal(previousTraceId, processedTransaction.Contexts.Trace.TraceId);
        Assert.Equal(previousSpanId, processedTransaction.Contexts.Trace.SpanId);
    }

    [Fact]
    public void OnStart_WithParentSpanId_StartsChildSpan()
    {
        // Arrange
        _fixture.Options.Instrumenter = Instrumenter.OpenTelemetry;
        var sut = _fixture.GetSut();

        using var parent = Tracer.StartActivity("Parent");
        sut.OnStart(parent);

        using var data = Tracer.StartActivity("TestActivity");

        // Act
        sut.OnStart(data!);

        // Assert
        Assert.True(sut.Map.TryGetValue(data.SpanId, out var span));
        using (new AssertionScope())
        {
            span.Should().BeOfType<SpanTracer>();
            span.SpanId.Should().Be(data.SpanId.AsSentrySpanId());
            span.ParentSpanId.Should().Be(data.ParentSpanId.AsSentrySpanId());
            if (span is not SpanTracer spanTracer)
            {
                Assert.Fail("Span is not a span tracer");
                return;
            }
            using (new AssertionScope())
            {
                spanTracer.SpanId.Should().Be(data.SpanId.AsSentrySpanId());
                spanTracer.ParentSpanId.Should().Be(data.ParentSpanId.AsSentrySpanId());
                spanTracer.TraceId.Should().Be(data.TraceId.AsSentryId());
                spanTracer.Operation.Should().Be(data.OperationName);
                spanTracer.Description.Should().Be(data.DisplayName);
                spanTracer.Status.Should().BeNull();
                spanTracer.StartTimestamp.Should().Be(data.StartTimeUtc);
            }
        }
    }

    [Fact]
    public void OnStart_WithoutParentSpanId_StartsNewTransaction()
    {
        // Arrange
        _fixture.Options.Instrumenter = Instrumenter.OpenTelemetry;
        var sut = _fixture.GetSut();

        var data = Tracer.StartActivity("test op");

        // Act
        sut.OnStart(data!);

        // Assert
        Assert.True(sut.Map.TryGetValue(data.SpanId, out var span));
        if (span is not TransactionTracer transaction)
        {
            Assert.Fail("Span is not a transaction tracer");
            return;
        }
        using (new AssertionScope())
        {
            transaction.SpanId.Should().Be(data.SpanId.AsSentrySpanId());
            transaction.ParentSpanId.Should().BeNull();
            transaction.TraceId.Should().Be(data.TraceId.AsSentryId());
            transaction.Name.Should().Be(data.DisplayName);
            transaction.Operation.Should().Be(data.OperationName);
            transaction.Description.Should().Be(data.DisplayName);
            transaction.Status.Should().BeNull();
            transaction.StartTimestamp.Should().Be(data.StartTimeUtc);
        }
    }

    [Fact]
    public void GetSpanStatus()
    {
        using (new AssertionScope())
        {
            var noAttributes = new Dictionary<string, object>();

            // Unset and OK -> OK
            SentrySpanProcessor.GetSpanStatus(ActivityStatusCode.Unset, noAttributes).Should().Be(SpanStatus.Ok);
            SentrySpanProcessor.GetSpanStatus(ActivityStatusCode.Ok, noAttributes).Should().Be(SpanStatus.Ok);

            // Error (no attributes) -> UnknownError
            SentrySpanProcessor.GetSpanStatus(ActivityStatusCode.Error, noAttributes)
                .Should().Be(SpanStatus.UnknownError);

            // Unknown status code -> UnknownError
            SentrySpanProcessor.GetSpanStatus((ActivityStatusCode)42, noAttributes)
                .Should().Be(SpanStatus.UnknownError);

            // We only test one http scenario, just to make sure the SpanStatusConverter is called for these headers.
            // Tests for SpanStatusConverter ensure other http status codes would also work though
            var notFoundAttributes = new Dictionary<string, object> { ["http.status_code"] = 404 };
            SentrySpanProcessor.GetSpanStatus(ActivityStatusCode.Error, notFoundAttributes)
                .Should().Be(SpanStatus.NotFound);

            // We only test one grpc scenario, just to make sure the SpanStatusConverter is called for these headers.
            // Tests for SpanStatusConverter ensure other grpc status codes would also work though
            var grpcAttributes = new Dictionary<string, object> { ["rpc.grpc.status_code"] = 7 };
            SentrySpanProcessor.GetSpanStatus(ActivityStatusCode.Error, grpcAttributes)
                .Should().Be(SpanStatus.PermissionDenied);
        }
    }
}
