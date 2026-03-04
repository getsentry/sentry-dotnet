using Sentry.Extensibility;
using Sentry.Internal;

namespace Sentry;

internal class SentryPropagationContext
{
    public SentryId TraceId { get; }
    public SpanId SpanId { get; }
    public SpanId? ParentSpanId { get; }

    public DynamicSamplingContext? DynamicSamplingContext { get; private set; }

    public DynamicSamplingContext GetOrCreateDynamicSamplingContext(SentryOptions options, IReplaySession replaySession)
    {
        if (DynamicSamplingContext is null)
        {
            options.LogDebug("Creating the Dynamic Sampling Context from the Propagation Context.");
            DynamicSamplingContext = this.CreateDynamicSamplingContext(options, replaySession);
        }

        return DynamicSamplingContext;
    }

    internal SentryPropagationContext(
        SentryId traceId,
        SpanId parentSpanId,
        DynamicSamplingContext? dynamicSamplingContext = null)
    {
        TraceId = traceId;
        SpanId = SpanId.Create();
        ParentSpanId = parentSpanId;
        DynamicSamplingContext = dynamicSamplingContext;
    }

    public SentryPropagationContext()
    {
        TraceId = SentryId.Create();
        SpanId = SpanId.Create();
    }

    public SentryPropagationContext(SentryPropagationContext? other)
    {
        TraceId = other?.TraceId ?? SentryId.Create();
        SpanId = other?.SpanId ?? SpanId.Create();
        ParentSpanId = other?.ParentSpanId;

        DynamicSamplingContext = other?.DynamicSamplingContext;
    }

    public static SentryPropagationContext CreateFromHeaders(IDiagnosticLogger? logger, SentryTraceHeader? traceHeader, BaggageHeader? baggageHeader, IReplaySession replaySession, string? sdkOrgId = null)
    {
        logger?.LogDebug("Creating a propagation context from headers.");

        if (traceHeader == null)
        {
            logger?.LogInfo("Sentry trace header is null. Creating new Sentry Propagation Context.");
            return new SentryPropagationContext();
        }

        // Check for org ID mismatch between SDK configuration and incoming baggage
        if (!string.IsNullOrEmpty(sdkOrgId) && baggageHeader is not null)
        {
            var sentryMembers = baggageHeader.GetSentryMembers();
            if (sentryMembers.TryGetValue("org_id", out var baggageOrgId)
                && !string.IsNullOrEmpty(baggageOrgId)
                && sdkOrgId != baggageOrgId)
            {
                logger?.LogInfo("Org ID mismatch (SDK: {0}, baggage: {1}). Starting new trace.", sdkOrgId, baggageOrgId);
                return new SentryPropagationContext();
            }
        }

        var dynamicSamplingContext = baggageHeader?.CreateDynamicSamplingContext(replaySession);
        return new SentryPropagationContext(traceHeader.TraceId, traceHeader.SpanId, dynamicSamplingContext);
    }
}
