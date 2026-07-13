using Sentry.Internal;
using Sentry.Internal.Tracing;
using Sentry.Maui.Internal;
using Sentry.Testing;

namespace Sentry.Maui.Tests;

/// <summary>
/// Spike (Q2): the same behavioral scenarios as <see cref="MauiActivityShimTests"/>, but driven through
/// <see cref="MauiUiActivityTracing"/> - the direct-Activity implementation with no shim, no
/// ITransactionTracer and no IHubInternal. Identical assertions passing against both implementations is
/// the evidence that removing the shim from the MAUI integration is a mechanical conversion.
/// </summary>
public class MauiDirectActivityTests : IDisposable
{
    private static readonly TimeSpan IdleTimeout = TimeSpan.FromSeconds(3);

    private readonly SentryOptions _options;
    private readonly ISentryClient _client;
    private readonly Hub _hub;
    private readonly SentryActivityListener _listener;
    private readonly MauiUiActivityTracing _tracing;
    private readonly List<MockTimer> _timers = new();

    public MauiDirectActivityTests()
    {
        _options = new SentryOptions
        {
            Dsn = ValidDsn,
            TracesSampleRate = 1.0,
            AutoSessionTracking = false,
            Instrumenter = Instrumenter.OpenTelemetry
        };
        _options.IdleTimerFactory = callback =>
        {
            var timer = new MockTimer(callback);
            _timers.Add(timer);
            return timer;
        };
        _client = Substitute.For<ISentryClient>();
        _hub = new Hub(_options, _client);
        // Filter by name: comparing to MauiUiActivityTracing.Source by reference races its static
        // initialization (ShouldListenTo fires from inside the ActivitySource constructor).
        _listener = new SentryActivityListener(_hub, s => s.Name == MauiUiActivityTracing.SourceName);
        _tracing = new MauiUiActivityTracing(IdleTimeout);
    }

    public void Dispose()
    {
        _listener.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void StartUiTransaction_CreatesIdleRootActivity()
    {
        // Act
        _tracing.StartUiTransaction("MainPage.loginButton");

        // Assert
        using (new AssertionScope())
        {
            var activity = _tracing.CurrentUiActivity;
            activity.Should().NotBeNull();
            activity!.OperationName.Should().Be(MauiEventsBinder.UserInteractionClickOp);
            activity.DisplayName.Should().Be("MainPage.loginButton");
            Activity.Current.Should().BeSameAs(activity);

            // The idle timeout fused onto the Activity reached the shadow tracer.
            _timers.Should().HaveCount(1);
            _timers[0].LastTimeout.Should().Be(IdleTimeout);
            _timers[0].IsCancelled.Should().BeFalse();
        }

        _tracing.CurrentUiActivity!.Stop();
    }

    [Fact]
    public void IdleTimeout_WithChildSpan_CapturesTrimmedTransaction_AndStopsActivity()
    {
        // Arrange
        _tracing.StartUiTransaction("MainPage.loginButton");
        var rootActivity = _tracing.CurrentUiActivity!;

        var navActivity = _tracing.StartNavigationSpan("LoginPage")!;
        navActivity.Stop(SpanStatus.Ok);

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

            rootActivity.IsStopped.Should().BeTrue();
        }
    }

    [Fact]
    public void StartNavigationSpan_ResetsIdleTimeout()
    {
        // Arrange
        _tracing.StartUiTransaction("MainPage.loginButton");
        var timer = _timers.Single();
        var startsBefore = timer.StartCount;

        // Act
        _tracing.StartNavigationSpan("LoginPage");

        // Assert
        timer.StartCount.Should().BeGreaterThan(startsBefore);

        _tracing.CurrentNavActivity!.Stop();
        _tracing.CurrentUiActivity!.Stop();
    }

    [Fact]
    public void IdleTimeout_NoChildSpans_DiscardsTransaction_AndStopsActivity()
    {
        // Arrange
        _tracing.StartUiTransaction("MainPage.loginButton");
        var rootActivity = _tracing.CurrentUiActivity!;

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
        _tracing.StartUiTransaction("MainPage.loginButton");
        var firstActivity = _tracing.CurrentUiActivity!;

        // Act: second click while the first transaction is still current.
        _tracing.StartUiTransaction("MainPage.logoutButton");
        var secondActivity = _tracing.CurrentUiActivity!;

        // Assert: the second transaction is an independent root (a new trace), NOT an implicit child of
        // the first activity - StartRootActivity explicitly detaches from the ambient Activity.
        using (new AssertionScope())
        {
            secondActivity.Should().NotBeSameAs(firstActivity);
            secondActivity.Parent.Should().BeNull();
            secondActivity.ParentSpanId.Should().Be(default(ActivitySpanId));
            secondActivity.TraceId.Should().NotBe(firstActivity.TraceId);
            _timers.Should().HaveCount(2);
        }

        // Both idle out: both are discarded (no children) and both activities stop.
        _timers[0].Fire();
        _timers[1].Fire();
        using (new AssertionScope())
        {
            firstActivity.IsStopped.Should().BeTrue();
            secondActivity.IsStopped.Should().BeTrue();
        }
    }
}
