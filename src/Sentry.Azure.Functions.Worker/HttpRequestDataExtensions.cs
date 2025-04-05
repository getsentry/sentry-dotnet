using Microsoft.Azure.Functions.Worker.Http;
using Sentry.Extensibility;

namespace Sentry.Azure.Functions.Worker;

internal static class HttpRequestDataExtensions
{
    public static SentryTraceHeader? TryGetSentryTraceHeader(this HttpRequestData context, IDiagnosticLogger? logger)
    {
        var traceHeaderValue = context.Headers.TryGetValues(SentryTraceHeader.HttpHeaderName, out var values)
            ? values.FirstOrDefault()
            : null;

        if (traceHeaderValue is null)
        {
            logger?.LogDebug("Did not receive a Sentry trace header.");
            return null;
        }

        logger?.LogDebug("Received Sentry trace header '{0}'.", traceHeaderValue);

        try
        {
            return SentryTraceHeader.Parse(traceHeaderValue);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Invalid Sentry trace header '{0}'.", traceHeaderValue);
            return null;
        }
    }

    public static W3CTraceHeader? TryGetW3CTraceHeader(this HttpRequestData context, IDiagnosticLogger? logger)
    {
        var traceHeaderValue = context.Headers.TryGetValues(W3CTraceHeader.HttpHeaderName, out var values)
            ? values.FirstOrDefault()
            : null;

        if (traceHeaderValue is null)
        {
            logger?.LogDebug("Did not receive a Sentry trace header.");
            return null;
        }

        logger?.LogDebug("Received Sentry trace header '{0}'.", traceHeaderValue);

        try
        {
            return W3CTraceHeader.Parse(traceHeaderValue);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Invalid Sentry trace header '{0}'.", traceHeaderValue);
            return null;
        }
    }

    public static BaggageHeader? TryGetBaggageHeader(this HttpRequestData context, IDiagnosticLogger? logger)
    {
        var baggageValue = context.Headers.TryGetValues(BaggageHeader.HttpHeaderName, out var value)
            ? value.FirstOrDefault()
            : null;

        if (baggageValue is null)
        {
            logger?.LogDebug("Did not receive a Sentry baggage header.");
            return null;
        }

        // Note: If there are multiple baggage headers, they will be joined with comma delimiters,
        // and can thus be treated as a single baggage header.

        logger?.LogDebug("Received baggage header '{0}'.", baggageValue);

        try
        {
            return BaggageHeader.TryParse(baggageValue, onlySentry: true);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Invalid baggage header '{0}'.", baggageValue);
            return null;
        }
    }
}
