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
}
