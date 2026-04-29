using Sentry.Extensibility;
using Sentry.Internal;

namespace Sentry.OpenTelemetry.Exporter;

/// <summary>
/// When using OpenTelemetry the TraceId etc come from the current Activity (which can be null).
/// </summary>
internal class OtelPropagationContext : IExternalPropagationContext
{
    public DynamicSamplingContext? DynamicSamplingContext { get; private set; }
    public SentryId? TraceId => Activity.Current?.TraceId.AsSentryId();
    public SpanId? SpanId => Activity.Current?.SpanId.AsSentrySpanId();
    public SpanId? ParentSpanId
    {
        get
        {
            var activity = Activity.Current;
            if (activity is null)
            {
                return null;
            }
            var parentSpanId = activity.ParentSpanId;
            return parentSpanId == default ? null : parentSpanId.AsSentrySpanId();
        }
    }

    public bool IsSampled => Activity.Current?.Recorded ?? false;

    public double? SampleRate
    {
        get
        {
            // TODO: Try to parse this from the `th` value in the TraceStateString
            // https://opentelemetry.io/docs/specs/otel/trace/tracestate-handling/#predefined-opentelemetry-sub-keys
            return null;
        }
    }

    public double? SampleRand
    {
        get
        {
            // TODO: Try to parse this from the `rv` value in the TraceStateString
            // https://opentelemetry.io/docs/specs/otel/trace/tracestate-handling/#predefined-opentelemetry-sub-keys
            return null;
        }
    }

    public BaggageHeader GetBaggageHeader()
    {
        var items = new Dictionary<string, string>();
        if (Activity.Current?.Baggage is {} baggage)
        {
            foreach (var item in baggage)
            {
                items.Add(item.Key, item.Value ?? string.Empty);
            }
        }
        return BaggageHeader.Create(items);
    }

    public DynamicSamplingContext? GetDynamicSamplingContext(SentryOptions options, IReplaySession replaySession)
    {
        if (DynamicSamplingContext is null)
        {
            options.LogDebug("Creating Dynamic Sampling Context from the External Propagation Context.");
            DynamicSamplingContext = this.CreateDynamicSamplingContext(options, replaySession);
        }

        return DynamicSamplingContext;
    }
}
