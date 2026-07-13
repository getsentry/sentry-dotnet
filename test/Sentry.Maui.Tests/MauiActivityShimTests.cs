using Sentry.Internal;
using Sentry.Internal.Tracing;
using Sentry.Maui.Internal;
using Sentry.Testing;

namespace Sentry.Maui.Tests;

/// <summary>
/// Battle-tests the Activity tracing shim against the MAUI automatic trace instrumentation from #5138:
/// the real <see cref="MauiEventsBinder"/> drives the real Hub with the shim installed, and the idle
/// transaction mechanics (idle timeout, reset-on-interaction, discard-if-empty, end-time trimming) must
/// all work while every transaction/span is backed by a System.Diagnostics.Activity.
/// </summary>
public class MauiActivityShimTests : IDisposable
{
    private readonly SentryMauiOptions _options;
    private readonly ISentryClient _client;
    private readonly Hub _hub;
    private readonly SentryActivityListener _listener;
    private readonly MauiEventsBinder _binder;
    private readonly List<MockTimer> _timers = new();

    public MauiActivityShimTests()
    {
        _options = new SentryMauiOptions
        {
            Dsn = ValidDsn,
            TracesSampleRate = 1.0,
            AutoSessionTracking = false,
            Instrumenter = Instrumenter.OpenTelemetry,
            AutoTransactionIdleTimeout = TimeSpan.FromSeconds(3)
        };
        _options.IdleTimerFactory = callback =>
        {
            var timer = new MockTimer(callback);
            _timers.Add(timer);
            return timer;
        };
        _client = Substitute.For<ISentryClient>();
        _hub = new Hub(_options, _client);
        _listener = new SentryActivityListener(_hub, s => s.Name == SentryActivitySources.ShimSourceName);
        _binder = new MauiEventsBinder(
            _hub,
            Microsoft.Extensions.Options.Options.Create(_options),
            []);
    }

    public void Dispose()
    {
        _listener.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void StartUiTransaction_ShimEnabled_CreatesActivityBackedIdleTransaction()
    {
        // Act
        _binder.StartUiTransaction("MainPage.loginButton");

        // Assert
        using (new AssertionScope())
        {
            // The binder went through IHubInternal.StartTransaction(context, idleTimeout) and got a shim back.
            _binder.CurrentUiTx.Should().BeOfType<ActivityTransactionShim>();

            var activity = Activity.Current;
            activity.Should().NotBeNull();
            activity!.OperationName.Should().Be(MauiEventsBinder.UserInteractionClickOp);
            activity.DisplayName.Should().Be("MainPage.loginButton");
            activity.Source.Name.Should().Be(SentryActivitySources.ShimSourceName);

            // The idle timeout survived the trip through the Activity side-channel: the shadow tracer
            // created by the processor is running a real idle timer with the configured timeout.
            _timers.Should().HaveCount(1);
            _timers[0].LastTimeout.Should().Be(_options.AutoTransactionIdleTimeout);
            _timers[0].IsCancelled.Should().BeFalse();
        }

        _binder.CurrentUiTx!.Finish();
    }

    [Fact]
    public void IdleTimeout_WithChildSpan_CapturesTrimmedTransaction_AndStopsActivity()
    {
        // Arrange
        _binder.StartUiTransaction("MainPage.loginButton");
        var rootActivity = Activity.Current!;

        // A navigation span goes through hub.GetSpan() -> scope -> shim, so it is Activity-backed too.
        // StartNavigationSpan also calls ResetIdleTimeout through the shim's IAutoTimeoutTracer.
        var navSpan = _binder.StartNavigationSpan("LoginPage")!;
        navSpan.Should().BeOfType<ActivitySpanShim>();
        navSpan.Finish(SpanStatus.Ok);

        // Act: the idle timeout elapses.
        _timers.Single().Fire();

        // Assert
        using (new AssertionScope())
        {
            _client.Received(1).CaptureTransaction(
                Arg.Is<SentryTransaction>(t =>
                    t.Name == "MainPage.loginButton" &&
                    t.Operation == MauiEventsBinder.UserInteractionClickOp &&
                    t.Spans.Count == 1 &&
                    t.Spans.Single().Operation == "ui.load" &&
                    t.Spans.Single().Description == "LoginPage" &&
                    // Idle transactions trim their end time to the last finished child span.
                    t.EndTimestamp == t.Spans.Single().EndTimestamp),
                Arg.Any<Scope>(),
                Arg.Any<SentryHint>());

            // The out-of-band (timer-driven) finish stopped the backing Activity, so it cannot leak or
            // stay Activity.Current and re-parent later spans.
            rootActivity.IsStopped.Should().BeTrue();
            Activity.Current.Should().BeNull();
        }
    }

    [Fact]
    public void StartNavigationSpan_ResetsIdleTimeout_ThroughShim()
    {
        // Arrange
        _binder.StartUiTransaction("MainPage.loginButton");
        var timer = _timers.Single();
        var startsBefore = timer.StartCount;

        // Act: the binder casts the transaction to IAutoTimeoutTracer - the shim must pass this through.
        _binder.StartNavigationSpan("LoginPage");

        // Assert
        timer.StartCount.Should().BeGreaterThan(startsBefore);

        _binder.CurrentNavSpan!.Finish();
        _binder.CurrentUiTx!.Finish();
    }

    [Fact]
    public void IdleTimeout_NoChildSpans_DiscardsTransaction_AndStopsActivity()
    {
        // Arrange
        _binder.StartUiTransaction("MainPage.loginButton");
        var rootActivity = Activity.Current!;

        // Act: idle timeout elapses with no child spans - the transaction is trivial and gets discarded.
        _timers.Single().Fire();

        // Assert
        using (new AssertionScope())
        {
            _client.DidNotReceive().CaptureTransaction(
                Arg.Any<SentryTransaction>(), Arg.Any<Scope>(), Arg.Any<SentryHint>());
            rootActivity.IsStopped.Should().BeTrue();
            _hub.GetTransaction().Should().BeNull();
        }
    }

    [Fact]
    public void SecondClick_StartsIndependentTransaction()
    {
        // Arrange: first click, transaction still idling (not finished).
        _binder.StartUiTransaction("MainPage.loginButton");
        var firstActivity = Activity.Current!;
        var firstTraceId = _binder.CurrentUiTx!.TraceId;

        // Act: second click while the first transaction is still current.
        _binder.StartUiTransaction("MainPage.logoutButton");
        var secondActivity = Activity.Current!;

        // Assert: the second transaction is an independent root (a new trace), NOT an implicit child of
        // the first activity - the shim explicitly detaches from the ambient Activity.
        using (new AssertionScope())
        {
            secondActivity.Should().NotBeSameAs(firstActivity);
            secondActivity.Parent.Should().BeNull();
            secondActivity.ParentSpanId.Should().Be(default(ActivitySpanId));
            _binder.CurrentUiTx.TraceId.Should().NotBe(firstTraceId);
            _timers.Should().HaveCount(2);
        }

        // Both idle out: the first is discarded (no children), the second too - and both activities stop.
        _timers[0].Fire();
        _timers[1].Fire();
        using (new AssertionScope())
        {
            firstActivity.IsStopped.Should().BeTrue();
            secondActivity.IsStopped.Should().BeTrue();
        }
    }
}
