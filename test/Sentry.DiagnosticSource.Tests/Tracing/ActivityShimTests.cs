using Sentry.Internal.Tracing;

namespace Sentry.DiagnosticSource.Tests.Tracing;

/// <summary>
/// End-to-end tests for the Activity tracing shim: Sentry-API calls (StartTransaction/StartChild/StartSpan)
/// create Activities under the hood, the SentryActivityListener converts them back into the transactions
/// that get captured, and pure-Activity instrumentation interleaves into the same trace.
/// </summary>
public class ActivityShimTests : IDisposable
{
    private readonly SentryOptions _options;
    private readonly ISentryClient _client;
    private readonly Hub _hub;
    private readonly ActivitySource _externalSource;
    private readonly SentryActivityListener _listener;

    public ActivityShimTests()
    {
        _options = new SentryOptions
        {
            Dsn = ValidDsn,
            TracesSampleRate = 1.0,
            AutoSessionTracking = false,
            Instrumenter = Instrumenter.OpenTelemetry
        };
        _client = Substitute.For<ISentryClient>();
        _hub = new Hub(_options, _client);
        _externalSource = new ActivitySource($"external-{Guid.NewGuid()}");
        _listener = new SentryActivityListener(_hub,
            s => s.Name == SentryActivitySources.ShimSourceName || s == _externalSource);
    }

    public void Dispose()
    {
        _listener.Dispose();
        _externalSource.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void StartTransaction_ShimEnabled_ReturnsActivityBackedTransaction()
    {
        // Act
        var transaction = _hub.StartTransaction("my name", "my operation");

        // Assert
        using (new AssertionScope())
        {
            transaction.Should().BeOfType<ActivityTransactionShim>();

            var activity = Activity.Current;
            activity.Should().NotBeNull();
            activity!.OperationName.Should().Be("my operation");
            activity.DisplayName.Should().Be("my name");
            activity.Source.Name.Should().Be(SentryActivitySources.ShimSourceName);

            transaction.TraceId.Should().Be(activity.TraceId.AsSentryId());
            transaction.SpanId.Should().Be(activity.SpanId.AsSentrySpanId());

            // The shim (not the shadow tracer) is on the scope, so integrations route through Activities.
            _hub.GetTransaction().Should().BeSameAs(transaction);
        }

        transaction.Finish();
    }

    [Fact]
    public void Finish_RichState_CapturedOnTransaction()
    {
        // Arrange
        var transaction = _hub.StartTransaction("original name", "some operation");
        transaction.SetTag("my.tag", "tag value");
        transaction.SetExtra("my.extra", "extra value");
        transaction.SetMeasurement("frames.dropped", 42);
        transaction.Name = "renamed";

        // Act: Cancelled has no ActivityStatusCode equivalent - it must survive via the side-channel.
        transaction.Finish(SpanStatus.Cancelled);

        // Assert
        transaction.IsFinished.Should().BeTrue();
        _client.Received(1).CaptureTransaction(
            Arg.Is<SentryTransaction>(t =>
                t.Name == "renamed" &&
                t.Operation == "some operation" &&
                t.Status == SpanStatus.Cancelled &&
                t.Tags["my.tag"] == "tag value" &&
                Equals(t.Data["my.extra"], "extra value") &&
                t.Measurements.ContainsKey("frames.dropped")),
            Arg.Any<Scope>(),
            Arg.Any<SentryHint>());
    }

    [Fact]
    public void StartChild_OnShim_CreatesActivityBackedChild()
    {
        // Arrange
        var transaction = _hub.StartTransaction("parent", "http.server");

        // Act
        var child = transaction.StartChild("db.query", "SELECT 1");
        var childActivity = Activity.Current;
        child.Finish();
        transaction.Finish();

        // Assert
        using (new AssertionScope())
        {
            child.Should().BeOfType<ActivitySpanShim>();
            childActivity!.OperationName.Should().Be("db.query");

            _client.Received(1).CaptureTransaction(
                Arg.Is<SentryTransaction>(t =>
                    t.Spans.Count == 1 &&
                    t.Spans.Single().Operation == "db.query" &&
                    t.Spans.Single().Description == "SELECT 1" &&
                    t.Spans.Single().ParentSpanId == t.Contexts.Trace.SpanId),
                Arg.Any<Scope>(),
                Arg.Any<SentryHint>());
        }
    }

    [Fact]
    public void ExternalActivity_ParentsUnderShimTransaction()
    {
        // This is the interleaving scenario the shim exists for: Sentry-API instrumentation and pure
        // Activity/OTel instrumentation (e.g. the MongoDB driver) land in one trace.

        // Arrange
        var transaction = _hub.StartTransaction("checkout", "http.server");
        var transactionSpanId = Activity.Current!.SpanId;

        // Act: an external library starts an Activity while the shim transaction is current.
        var externalActivity = _externalSource.StartActivity("mongodb.find")!;
        externalActivity.Stop();
        transaction.Finish();

        // Assert
        using (new AssertionScope())
        {
            externalActivity.ParentSpanId.Should().Be(transactionSpanId);
            _client.Received(1).CaptureTransaction(
                Arg.Is<SentryTransaction>(t =>
                    t.Name == "checkout" &&
                    t.Spans.Count == 1 &&
                    t.Spans.Single().Operation == "mongodb.find" &&
                    t.Spans.Single().ParentSpanId == t.Contexts.Trace.SpanId),
                Arg.Any<Scope>(),
                Arg.Any<SentryHint>());
        }
    }

    [Fact]
    public void HubStartSpan_RoutesThroughActivity()
    {
        // Integrations use hub.GetSpan()/hub.StartSpan rather than holding transaction references -
        // this validates that the scope hand-off routes those through Activities too.

        // Arrange
        var transaction = _hub.StartTransaction("parent", "http.server");

        // Act
        var span = _hub.StartSpan("child.op", "child description");
        span.Finish();
        transaction.Finish();

        // Assert
        using (new AssertionScope())
        {
            span.Should().BeOfType<ActivitySpanShim>();
            _client.Received(1).CaptureTransaction(
                Arg.Is<SentryTransaction>(t =>
                    t.Spans.Count == 1 &&
                    t.Spans.Single().Operation == "child.op"),
                Arg.Any<Scope>(),
                Arg.Any<SentryHint>());
        }
    }

    [Fact]
    public void StartTransaction_Unsampled_NothingCaptured()
    {
        // Arrange
        _options.TracesSampleRate = 0.0;

        // Act
        var transaction = _hub.StartTransaction("unsampled", "op");
        var child = transaction.StartChild("child.op");
        child.Finish();
        transaction.Finish();

        // Assert
        using (new AssertionScope())
        {
            transaction.Should().BeOfType<ActivityTransactionShim>();
            transaction.IsSampled.Should().Be(false);
            _client.DidNotReceive().CaptureTransaction(
                Arg.Any<SentryTransaction>(), Arg.Any<Scope>(), Arg.Any<SentryHint>());
        }
    }

    [Fact]
    public void CustomSamplingContext_ReachesTracesSampler()
    {
        // The customSamplingContext dictionary has no channel through ActivityListener.Sample - the shim
        // carries it as a side-channel on the Activity, and the processor hands it back to Sentry's
        // sampling pipeline. This test proves that round-trip.

        // Arrange
        _options.TracesSampleRate = null;
        _options.TracesSampler = context =>
            context.CustomSamplingContext.TryGetValue("vip", out var vip) && Equals(vip, true) ? 1.0 : 0.0;

        // Act
        var vipTransaction = _hub.StartTransaction(
            new TransactionContext("vip request", "http.server"),
            new Dictionary<string, object> { ["vip"] = true });
        var regularTransaction = _hub.StartTransaction(
            new TransactionContext("regular request", "http.server"),
            new Dictionary<string, object> { ["vip"] = false });

        // Assert
        using (new AssertionScope())
        {
            vipTransaction.IsSampled.Should().Be(true);
            regularTransaction.IsSampled.Should().Be(false);
        }

        vipTransaction.Finish();
        regularTransaction.Finish();
    }

    [Fact]
    public void StartTransaction_NoListener_FallsBackToClassicTracing()
    {
        // Arrange: the factory is installed but no listener is subscribed to the Sentry ActivitySource
        // (e.g. it was disposed) - CreateActivity returns null and Hub falls through to the classic path.
        _listener.Dispose();
        _options.ActivityShimFactory = ActivityTransactionShim.Create;

        // Act
        var transaction = _hub.StartTransaction("classic", "op");

        // Assert
        transaction.Should().BeOfType<TransactionTracer>();
        transaction.Finish();
    }
}
