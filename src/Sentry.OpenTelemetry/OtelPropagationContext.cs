using Sentry.Extensibility;
using Sentry.Internal;

namespace Sentry.OpenTelemetry;

internal class OtelPropagationContext : IPropagationContext
{
    public DynamicSamplingContext? DynamicSamplingContext { get; private set; }

    public SentryId TraceId => Activity.Current?.TraceId.AsSentryId() ?? default;
    public SpanId SpanId => Activity.Current?.SpanId.AsSentrySpanId() ?? default;
    public SpanId? ParentSpanId => Activity.Current?.ParentSpanId.AsSentrySpanId();

    public DynamicSamplingContext GetOrCreateDynamicSamplingContext(SentryOptions options, IReplaySession replaySession)
    {
        if (DynamicSamplingContext is null)
        {
            options.LogDebug("Creating the Dynamic Sampling Context from the Propagation Context.");
            DynamicSamplingContext = this.CreateDynamicSamplingContext(options, replaySession);
        }

        return DynamicSamplingContext;
    }
}
