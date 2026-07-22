using Sentry.Internal.Tracing;

namespace Sentry.DiagnosticSource.Tests.Tracing;

/// <summary>
/// End-to-end tests for <see cref="SentryActivityListener"/>: Activities created via ActivitySource are
/// captured as Sentry transactions/spans with no OpenTelemetry SDK involved — the listener's own
/// Sample/ActivityStarted/ActivityStopped callbacks drive the whole pipeline.
/// </summary>
public class SentryActivityListenerTests
{
    private class Fixture
    {
        public SentryOptions Options { get; }
        public ISentryClient Client { get; }

        public Fixture()
        {
            Options = new SentryOptions
            {
                Dsn = ValidDsn,
                TracesSampleRate = 1.0,
                AutoSessionTracking = false,
                Instrumenter = Instrumenter.OpenTelemetry
            };
            Client = Substitute.For<ISentryClient>();
        }

        public Hub Hub { get; private set; }

        public Hub GetHub() => Hub ??= new Hub(Options, Client);
    }

    private readonly Fixture _fixture = new();

    [Fact]
    public void ActivityStopped_RootActivity_CapturesTransaction()
    {
        // Arrange
        var hub = _fixture.GetHub();
        using var source = new ActivitySource($"{nameof(SentryActivityListenerTests)}-{Guid.NewGuid()}");
        using var listener = new SentryActivityListener(hub, s => s == source);

        // Act
        var activity = source.StartActivity("test operation")!;
        activity.DisplayName = "test display name";
        activity.Stop();

        // Assert
        _fixture.Client.Received(1).CaptureTransaction(
            Arg.Is<SentryTransaction>(t =>
                t.Name == "test display name" &&
                t.Operation == "test operation" &&
                t.IsSampled == true),
            Arg.Any<Scope>(),
            Arg.Any<SentryHint>());
    }

    [Fact]
    public void ActivityStopped_ChildActivity_CapturesSpanOnTransaction()
    {
        // Arrange
        var hub = _fixture.GetHub();
        using var source = new ActivitySource($"{nameof(SentryActivityListenerTests)}-{Guid.NewGuid()}");
        using var listener = new SentryActivityListener(hub, s => s == source);

        // Act
        var parent = source.StartActivity("parent operation")!;
        var child = source.StartActivity("child operation")!;
        child.Stop();
        parent.Stop();

        // Assert
        _fixture.Client.Received(1).CaptureTransaction(
            Arg.Is<SentryTransaction>(t =>
                t.Operation == "parent operation" &&
                t.Spans.Count == 1 &&
                t.Spans.Single().Operation == "child operation" &&
                t.Spans.Single().ParentSpanId == t.Contexts.Trace.SpanId),
            Arg.Any<Scope>(),
            Arg.Any<SentryHint>());
    }

    [Fact]
    public void ShouldListenTo_OtherSource_IsIgnored()
    {
        // Arrange
        var hub = _fixture.GetHub();
        using var source = new ActivitySource($"{nameof(SentryActivityListenerTests)}-{Guid.NewGuid()}");
        using var otherSource = new ActivitySource($"other-{Guid.NewGuid()}");
        using var listener = new SentryActivityListener(hub, s => s == source);

        // Act
        // No other listener is subscribed to otherSource, so StartActivity returns null — exactly the
        // ActivitySource.HasListeners() zero-cost property the migration relies on.
        var activity = otherSource.StartActivity("ignored operation");

        // Assert
        activity.Should().BeNull();
        _fixture.Client.DidNotReceive().CaptureTransaction(
            Arg.Any<SentryTransaction>(), Arg.Any<Scope>(), Arg.Any<SentryHint>());
    }

    [Fact]
    public void ActivityStopped_UnsampledActivity_DoesNotCaptureTransaction()
    {
        // Arrange
        _fixture.Options.TracesSampleRate = 0.0;
        var hub = _fixture.GetHub();
        using var source = new ActivitySource($"{nameof(SentryActivityListenerTests)}-{Guid.NewGuid()}");
        using var listener = new SentryActivityListener(hub, s => s == source);

        // Act
        var activity = source.StartActivity("test operation")!;
        activity.Stop();

        // Assert
        _fixture.Client.DidNotReceive().CaptureTransaction(
            Arg.Any<SentryTransaction>(), Arg.Any<Scope>(), Arg.Any<SentryHint>());
    }
}
