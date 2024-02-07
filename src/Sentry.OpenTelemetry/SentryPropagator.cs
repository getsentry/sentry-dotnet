using Microsoft.Extensions.Primitives;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using Sentry.Extensibility;

namespace Sentry.OpenTelemetry;

/// <summary>
/// Sentry OpenTelemetry Propagator.
/// Injects and extracts both <c>sentry-trace</c> and <c>baggage</c> headers from carriers.
/// </summary>
public class SentryPropagator : BaggagePropagator
{
    private readonly IHub? _hub;
    private IHub Hub => _hub ?? SentrySdk.CurrentHub;
    private SentryOptions? Options => Hub.GetSentryOptions();

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
        Options?.LogDebug("SentryPropagator.Extract");

        var result = base.Extract(context, carrier, getter);
        var baggage = result.Baggage; // The Otel .NET SDK takes care of baggage headers alread

        Options?.LogDebug("Baggage");
        foreach (var entry in baggage)
        {
            Options?.LogDebug(entry.ToString());
        }

        if (TryGetSentryTraceHeader(carrier, getter) is not { } sentryTraceHeader)
        {
            Options?.LogDebug("No SentryTraceHeader present in carrier");
            return result;
        }

        Options?.LogDebug($"Extracted SentryTraceHeader from carrier: {sentryTraceHeader}");

        var activityContext = new ActivityContext(
            sentryTraceHeader.TraceId.AsActivityTraceId(),
            sentryTraceHeader.SpanId.AsActivitySpanId(),
            // NOTE: Our Java and JavaScript SDKs set sentryTraceHeader.IsSampled = true if any trace header is present.
            sentryTraceHeader.IsSampled is true ? ActivityTraceFlags.Recorded : ActivityTraceFlags.None,
            traceState: null, // See https://www.w3.org/TR/trace-context/#design-overview
            isRemote: true
            );
        return new PropagationContext(activityContext, baggage);
    }

    /// <inheritdoc />
    public override void Inject<T>(PropagationContext context, T carrier, Action<T, string, string> setter)
    {
        Options?.LogDebug("SentryPropagator.Inject");

        // Don't inject if instrumentation is suppressed
        if (Sdk.SuppressInstrumentation)
        {
            Options?.LogDebug("Not injecting Sentry tracing information. Instrumentation is suppressed.");
            return;
        }

        // Don't inject when the activity context is invalid.
        if (!context.ActivityContext.IsValid())
        {
            Options?.LogDebug("Not injecting Sentry tracing information for invalid activity context.");
            return;
        }

        // Don't inject if this is a request to the Sentry ingest endpoint.
        if (carrier is HttpRequestMessage request && (Options?.IsSentryRequest(request.RequestUri) ?? false))
        {
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
        Options?.LogDebug($"SentryTraceHeader: {traceHeader}");
        setter(carrier, SentryTraceHeader.HttpHeaderName, traceHeader.ToString());

        base.Inject(context, carrier, setter);
    }

    private static SentryTraceHeader? TryGetSentryTraceHeader<T>(T carrier, Func<T, string, IEnumerable<string>> getter)
    {
        var headerValue = getter(carrier, SentryTraceHeader.HttpHeaderName);
        try
        {
            var value = new StringValues(headerValue.ToArray());
            return SentryTraceHeader.Parse(value);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
