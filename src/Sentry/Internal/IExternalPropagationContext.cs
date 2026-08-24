namespace Sentry.Internal;

internal interface IExternalPropagationContext
{
    public DynamicSamplingContext? DynamicSamplingContext { get; }
    public SentryId? TraceId { get; }
    public SpanId? SpanId { get; }
    public SpanId? ParentSpanId { get; }
    public bool IsSampled { get; }
    public double? SampleRate { get; }
    public double? SampleRand { get; }

    public BaggageHeader GetBaggageHeader();

    public DynamicSamplingContext? GetDynamicSamplingContext(SentryOptions options, IReplaySession replaySession);

    /// <summary>
    /// Returns a snapshot of this context with all values fixed at the current instant.
    /// Non-Activity-based implementations can return <c>this</c> since their values are already stable.
    /// </summary>
#if NET5_0_OR_GREATER || NETSTANDARD2_1
    public IExternalPropagationContext Snapshot() => this;
#else
    public IExternalPropagationContext Snapshot();
#endif
}
