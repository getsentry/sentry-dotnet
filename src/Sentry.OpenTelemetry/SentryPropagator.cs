using Microsoft.Extensions.Primitives;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

namespace Sentry.OpenTelemetry;

/// <summary>
/// Sentry OpenTelemetry Propagator.
/// Injects and extracts both <c>sentry-trace</c> and <c>baggage</c> headers from carriers.
/// </summary>
public class SentryPropagator : BaggagePropagator
{
    /// <inheritdoc />
    public override ISet<string> Fields => new HashSet<string>
    {
        SentryTraceHeader.HttpHeaderName,
        BaggageHeader.HttpHeaderName
    };

    private static class OTelKeys
    {
        public const string SentryBaggageKey = "sentry.baggage";
        public const string SentryTraceKey = "sentry.trace";
    }

    /// <inheritdoc />
    public override PropagationContext Extract<T>(PropagationContext context, T carrier, Func<T, string, IEnumerable<string>> getter)
    {
        var result = base.Extract(context, carrier, getter);
        var baggage = result.Baggage; // The Otel .NET SDK takes care of baggage headers alread

        if (TryGetSentryTraceHeader(carrier, getter) is not {} sentryTraceHeader)
        {
            return result;
        }
        var activityContext = new ActivityContext(
            sentryTraceHeader.TraceId.AsActivityTraceId(),
            sentryTraceHeader.SpanId.AsActivitySpanId(),
            sentryTraceHeader.IsSampled is true ? ActivityTraceFlags.Recorded : ActivityTraceFlags.None,
            null,
            true
            );
        return new PropagationContext(activityContext, baggage);
    }

    /// <inheritdoc />
    public override void Inject<T>(PropagationContext context, T carrier, Action<T, string, string> setter)
    {

        // Don't inject if instrumentation is suppressed, or when the activity context is invalid.
        if (Sdk.SuppressInstrumentation || !context.ActivityContext.IsValid())
        {
            return;
        }

        // Don't inject if this is a request to the Sentry ingest endpoint.
        if (carrier is HttpRequestMessage request && SentrySdk.CurrentHub.IsSentryRequest(request.RequestUri))
        {
            return;
        }

        // Set the sentry trace header for downstream requests
        var traceId = context.ActivityContext.TraceId.AsSentryId();
        var spanSpanId = context.ActivityContext.SpanId.AsSentrySpanId();
        var isSampled = context.ActivityContext.TraceFlags.HasFlag(ActivityTraceFlags.Recorded);
        var traceHeader = new SentryTraceHeader(traceId, spanSpanId, isSampled);
        setter(carrier, SentryTraceHeader.HttpHeaderName, traceHeader.ToString());

        base.Inject(context, carrier, setter);
    }

    private static SentryTraceHeader? TryGetSentryTraceHeader<T>(T carrier, Func<T, string, IEnumerable<string>> getter)
    {
        var headerValue = getter(carrier, SentryTraceHeader.HttpHeaderName);
        var value = new StringValues(headerValue.ToArray());
        try
        {
            return SentryTraceHeader.Parse(value);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
