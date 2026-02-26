namespace Sentry.Internal;

internal interface IPropagationContext
{
    public DynamicSamplingContext? DynamicSamplingContext { get; }

    public SentryId TraceId { get; }
    public SpanId SpanId { get; }
    public SpanId? ParentSpanId { get; }

    public DynamicSamplingContext GetOrCreateDynamicSamplingContext(SentryOptions options, IReplaySession replaySession);
}
