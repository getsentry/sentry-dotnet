using Sentry.Extensibility;
using Sentry.Internal;

namespace Sentry.OpenTelemetry.Exporter.OpenTelemetryProtocol;

internal class OtelPropagationContext : IPropagationContext
{
    public DynamicSamplingContext? DynamicSamplingContext { get; private set; }

    public SentryId TraceId => Activity.Current?.TraceId.AsSentryId() ?? default;
    public SpanId SpanId => Activity.Current?.SpanId.AsSentrySpanId() ?? default;
    public SpanId? ParentSpanId => Activity.Current?.ParentSpanId.AsSentrySpanId();

    /// <summary>
    /// Warning: this method may throw an exception if Activity.Current is null.
    /// This method should not be used when instrumenting with OTEL.
    /// </summary>
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
