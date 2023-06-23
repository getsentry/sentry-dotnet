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

        var modifiedContext = result.Baggage;

        if (TryGetSentryTraceHeader(carrier, getter) is not {} sentryTraceHeader)
        {
            return result;
        }
        modifiedContext.SetBaggage(OTelKeys.SentryTraceKey, sentryTraceHeader.ToString());

        var baggageHeader = TryGetBaggageHeader(carrier, getter)
                            ?? BaggageHeader.Create(new List<KeyValuePair<string, string>>());
        modifiedContext.SetBaggage(OTelKeys.SentryBaggageKey, baggageHeader.ToString());

        var otelSpanContext = new ActivityContext(
            sentryTraceHeader.TraceId.AsActivityTraceId(),
            sentryTraceHeader.SpanId.AsActivitySpanId(),
            sentryTraceHeader.IsSampled is true ? ActivityTraceFlags.Recorded : ActivityTraceFlags.None,
            null,
            true
            );

        // TODO: Understand why this is needed in the Java implementation. Maybe we don't need in .NET as we can pass
        // this directly into the constructor of the PropagationContext

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

    private static BaggageHeader? TryGetBaggageHeader<T>(T carrier, Func<T, string, IEnumerable<string>> getter)
    {
        var headerValue = getter(carrier, BaggageHeader.HttpHeaderName);
        var value = new StringValues(headerValue.ToArray());
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
