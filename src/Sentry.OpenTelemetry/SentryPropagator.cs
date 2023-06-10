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

    /// <inheritdoc />
    public override PropagationContext Extract<T>(PropagationContext context, T carrier, Func<T, string, IEnumerable<string>> getter)
    {
        var result = base.Extract(context, carrier, getter);
        var baggage = result.Baggage;

        // TODO: Finish

        return result;
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

        base.Inject(context, carrier, setter);
    }
}
