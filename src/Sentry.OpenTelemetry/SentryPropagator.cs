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
    private readonly IHub _hub = SentrySdk.CurrentHub;

    /// <summary>
    /// <para>
    ///     Creates a new SentryPropagator.
    /// </para>
    /// <para>
    ///     You should register the propagator with the OpenTelemetry SDK when initializing your application.
    /// </para>
    /// <example>
    ///     OpenTelemetry.Sdk.SetDefaultTextMapPropagator(new SentryPropagator());
    /// </example>
    /// </summary>
    public SentryPropagator() : base()
    {
    }

    internal SentryPropagator(IHub hub) : this()
    {
        _hub = hub;
    }
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
        Debug.WriteLine("SentryPropagator.Extract");

        var result = base.Extract(context, carrier, getter);
        var baggage = result.Baggage; // The Otel .NET SDK takes care of baggage headers alread

        Debug.WriteLine("Baggage");
        foreach (var entry in baggage)
        {
            Debug.WriteLine(entry.ToString());
        }

        if (TryGetSentryTraceHeader(carrier, getter) is not {} sentryTraceHeader)
        {
            Debug.WriteLine("No SentryTraceHeader present in carrier");
            return result;
        }

        Debug.WriteLine($"Extracted SentryTraceHeader from carrier: {sentryTraceHeader}");

        var activityContext = new ActivityContext(
            sentryTraceHeader.TraceId.AsActivityTraceId(),
            sentryTraceHeader.SpanId.AsActivitySpanId(),
            sentryTraceHeader.IsSampled is true ? ActivityTraceFlags.Recorded : ActivityTraceFlags.None,
            null, // See https://www.w3.org/TR/trace-context/#design-overview
            true
            );
        return new PropagationContext(activityContext, baggage);
    }

    /// <inheritdoc />
    public override void Inject<T>(PropagationContext context, T carrier, Action<T, string, string> setter)
    {
        Debug.WriteLine("SentryPropagator.Inject");

        // Don't inject if instrumentation is suppressed, or when the activity context is invalid.
        if (Sdk.SuppressInstrumentation || !context.ActivityContext.IsValid())
        {
            Debug.WriteLine("Injection skipped (suppressed or invalid context).");
            return;
        }

        // Don't inject if this is a request to the Sentry ingest endpoint.
        if (carrier is HttpRequestMessage request && _hub.IsSentryRequest(request.RequestUri))
        {
            Debug.WriteLine("Injection skipped for Sentry ingest.");
            return;
        }

        // Reconstruct SentryTraceHeader from the OpenTelemetry activity/span context
        // TODO: Check if this is correct. Although the TraceId will be retained, the SpanId may change. Is that how it's supposed to work?
        var traceHeader = new SentryTraceHeader(
            context.ActivityContext.TraceId.AsSentryId(),
            context.ActivityContext.SpanId.AsSentrySpanId(),
            context.ActivityContext.TraceFlags.HasFlag(ActivityTraceFlags.Recorded)
            );

        // Set the sentry trace header for downstream requests
        Debug.WriteLine($"SentryTraceHeader: {traceHeader}");
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
