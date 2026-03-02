using Sentry.Extensibility;
using Sentry.Internal;

namespace Sentry;

internal class SentryPropagationContext
{
    public SentryId TraceId { get; }
    public SpanId SpanId { get; }
    public SpanId? ParentSpanId { get; }

    internal DynamicSamplingContext? _dynamicSamplingContext;

    public DynamicSamplingContext GetOrCreateDynamicSamplingContext(SentryOptions options, IReplaySession replaySession)
    {
        if (_dynamicSamplingContext is null)
        {
            options.LogDebug("Creating the Dynamic Sampling Context from the Propagation Context.");
            _dynamicSamplingContext = this.CreateDynamicSamplingContext(options, replaySession);
        }

        return _dynamicSamplingContext;
    }

    internal SentryPropagationContext(
        SentryId traceId,
        SpanId parentSpanId,
        DynamicSamplingContext? dynamicSamplingContext = null)
    {
        TraceId = traceId;
        SpanId = SpanId.Create();
        ParentSpanId = parentSpanId;
        _dynamicSamplingContext = dynamicSamplingContext;
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

        _dynamicSamplingContext = other?._dynamicSamplingContext;
    }

    public static SentryPropagationContext CreateFromHeaders(IDiagnosticLogger? logger, SentryTraceHeader? traceHeader, BaggageHeader? baggageHeader, IReplaySession replaySession)
    {
        return CreateFromHeaders(logger, traceHeader, baggageHeader, replaySession, null);
    }

    public static SentryPropagationContext CreateFromHeaders(IDiagnosticLogger? logger, SentryTraceHeader? traceHeader, BaggageHeader? baggageHeader, IReplaySession replaySession, SentryOptions? options)
    {
        logger?.LogDebug("Creating a propagation context from headers.");

        if (traceHeader == null)
        {
            logger?.LogInfo("Sentry trace header is null. Creating new Sentry Propagation Context.");
            return new SentryPropagationContext();
        }

        if (options != null && !ShouldContinueTrace(options, baggageHeader))
        {
            logger?.LogDebug("Not continuing trace due to org ID mismatch.");
            return new SentryPropagationContext();
        }

        var dynamicSamplingContext = baggageHeader?.CreateDynamicSamplingContext(replaySession);
        return new SentryPropagationContext(traceHeader.TraceId, traceHeader.SpanId, dynamicSamplingContext);
    }

    internal static bool ShouldContinueTrace(SentryOptions options, BaggageHeader? baggageHeader)
    {
        var sdkOrgId = options.EffectiveOrgId;

        string? baggageOrgId = null;
        if (baggageHeader != null)
        {
            var sentryMembers = baggageHeader.GetSentryMembers();
            sentryMembers.TryGetValue("org_id", out baggageOrgId);
        }

        // Mismatched org IDs always reject regardless of strict mode
        if (sdkOrgId != null && baggageOrgId != null && sdkOrgId != baggageOrgId)
        {
            return false;
        }

        // In strict mode, both must be present and match (unless both are missing)
        if (options.StrictTraceContinuation)
        {
            if (sdkOrgId == null && baggageOrgId == null)
            {
                return true;
            }
            return sdkOrgId != null && sdkOrgId == baggageOrgId;
        }

        return true;
    }
}
