#if HAS_ACTIVITY_LISTENER
namespace Sentry.Internal.Tracing;

/// <summary>
/// Subscribes to <see cref="Activity"/> instrumentation via <see cref="ActivityListener"/> (part of the .NET
/// runtime — no OpenTelemetry SDK dependency) and forwards activity lifecycle events to a
/// <see cref="SentryActivityProcessor"/>, which converts them into Sentry transactions and spans.
/// </summary>
/// <remarks>
/// This replaces the registration glue that Sentry.OpenTelemetry gets from the OpenTelemetry SDK
/// (TracerProviderBuilder.AddProcessor): where the OTel SDK decides which ActivitySources to listen to and
/// which activities to sample, here those decisions are made by the <c>shouldListenTo</c> predicate and the
/// Sample callback respectively.
///
/// Spike note on sampling: for parity with the current POTEL behaviour (where the OTel SDK records everything
/// and Sentry re-runs its own sampling when the root span is converted in
/// <c>SentryActivityProcessor.CreateRootSpan</c>), the Sample callback returns AllDataAndRecorded for
/// every activity. A production implementation could instead invoke Sentry's sampling logic here — at activity
/// creation time — to avoid paying for recording of activities that Sentry will discard. That requires solving
/// the TracesSampler customSamplingContext gap (ActivityCreationOptions has no channel for it) and reconciling
/// with any other ActivityListeners present (the runtime takes the most permissive sampling result).
/// </remarks>
internal sealed class SentryActivityListener : IDisposable
{
    private readonly ActivityListener _listener;

    internal SentryActivityProcessor Processor { get; }

    public SentryActivityListener(
        IHub hub,
        Func<ActivitySource, bool>? shouldListenTo = null,
        IEnumerable<ISentryActivityEnricher>? enrichers = null,
        IReplaySession? replaySession = null,
        Func<IDictionary<string, object>>? resourceAttributeResolver = null)
    {
        Processor = new SentryActivityProcessor(hub, enrichers, replaySession, resourceAttributeResolver);
        _listener = new ActivityListener
        {
            ShouldListenTo = source => shouldListenTo?.Invoke(source) ?? true,
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) =>
                ActivitySamplingResult.AllDataAndRecorded,
            SampleUsingParentId = static (ref ActivityCreationOptions<string> _) =>
                ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = Processor.OnStart,
            ActivityStopped = Processor.OnEnd
        };
        ActivitySource.AddActivityListener(_listener);
    }

    public void Dispose() => _listener.Dispose();
}
#endif
