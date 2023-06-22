using Microsoft.AspNetCore.Http;
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

        if (carrier is not HttpRequest request)
        {
            return result;
        }

        var modifiedContext = result.Baggage;

        if (TryGetSentryTraceHeader(request.HttpContext) is not {} sentryTraceHeader)
        {
            return result;
        }
        modifiedContext.SetBaggage(OTelKeys.SentryTraceKey, sentryTraceHeader.ToString());

        // Sentry uses the word Baggage (https://develop.sentry.dev/sdk/performance/dynamic-sampling-context/#baggage)
        // to mean something a bit different to OTEL (https://opentelemetry.io/docs/concepts/signals/baggage/)... so
        // the names here are a bit confusing.
        var baggageHeader = TryGetBaggageHeader(request.HttpContext)
                            ?? BaggageHeader.Create(new List<KeyValuePair<string, string>>());
        modifiedContext.SetBaggage(OTelKeys.SentryBaggageKey, baggageHeader.ToString());

        var otelSpanContext = new ActivityContext(
            sentryTraceHeader.TraceId.AsActivityTraceId(),
            sentryTraceHeader.SpanId.AsActivitySpanId(),
            sentryTraceHeader.IsSampled is true ? ActivityTraceFlags.Recorded : ActivityTraceFlags.None,
            null,
            true
            );

        // TODO: Understand why this is needed in the Java implementation
        // Span wrappedSpan = Span.wrap(otelSpanContext);
        // modifiedContext = modifiedContext.with(wrappedSpan);
        // return modifiedContext;

        return new PropagationContext(otelSpanContext, modifiedContext);
    }

    /// <inheritdoc />
    public override void Inject<T>(PropagationContext context, T carrier, Action<T, string, string> setter)
    {

        // Don't inject baggage if instrumentation is suppressed, or when the activity context is invalid.
        if (Sdk.SuppressInstrumentation || !context.ActivityContext.IsValid())
        {
            return;
        }

        // Don't inject baggage if this is a request to the Sentry ingest endpoint.
        if (carrier is HttpRequestMessage request && SentrySdk.CurrentHub.IsSentryRequest(request.RequestUri))
        {
            return;
        }

        var baggage = context.Baggage;

        // TODO: Finish

        // Java implementation
        // ===================
        // Span otelSpan = Span.fromContext(context);
        // SpanContext otelSpanContext = otelSpan.getSpanContext();
        //
        // ISpan sentrySpan = spanStorage.get(otelSpanContext.getSpanId());
        // if (sentrySpan == null || sentrySpan.isNoOp())
        //     return;
        //
        // SentryTraceHeader sentryTraceHeader = sentrySpan.toSentryTrace();
        // setter.set(carrier, sentryTraceHeader.getName(), sentryTraceHeader.getValue());
        // BaggageHeader baggageHeader = sentrySpan.toBaggageHeader(Collections.emptyList());
        // if (baggageHeader != null)
        //     setter.set(carrier, baggageHeader.getName(), baggageHeader.getValue());

        base.Inject(context, carrier, setter);
    }

    private static SentryTraceHeader? TryGetSentryTraceHeader(HttpContext context)
    {
        context.Request.Headers.TryGetValue(SentryTraceHeader.HttpHeaderName, out var value);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        try
        {
            return SentryTraceHeader.Parse(value);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static BaggageHeader? TryGetBaggageHeader(HttpContext context)
    {
        context.Request.Headers.TryGetValue(BaggageHeader.HttpHeaderName, out var value);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        // Note: If there are multiple baggage headers, they will be joined with comma delimiters,
        // and can thus be treated as a single baggage header.
        try
        {
            return BaggageHeader.TryParse(value, onlySentry: true);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
