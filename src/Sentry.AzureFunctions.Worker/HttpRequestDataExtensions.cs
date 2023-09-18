using Microsoft.Azure.Functions.Worker.Http;

namespace Sentry.AzureFunctions.Worker;

internal static class HttpRequestDataExtensions
{
    public static SentryTraceHeader? TryGetSentryTraceHeader(this HttpRequestData context, SentryOptions? options)
    {
        if (!context.Headers.TryGetValues(SentryTraceHeader.HttpHeaderName, out var values))
        {
            return null;
        }

        var traceHeaderValue = values.FirstOrDefault();
        if (traceHeaderValue is null)
        {
            return null;
        }

        // options?.LogDebug("Received Sentry trace header '{0}'.", value);

        try
        {
            return SentryTraceHeader.Parse(traceHeaderValue);
        }
        // catch (Exception ex)
        catch (Exception)
        {
            // options?.LogError("Invalid Sentry trace header '{0}'.", ex, value);
            return null;
        }
    }

    public static BaggageHeader? TryGetBaggageHeader(this HttpRequestData context, SentryOptions? options)
    {
        if (!context.Headers.TryGetValues(BaggageHeader.HttpHeaderName, out var value))
        {
            return null;
        }

        var baggageValue = value.FirstOrDefault();
        if (baggageValue is null)
        {
            return null;
        }

        // Note: If there are multiple baggage headers, they will be joined with comma delimiters,
        // and can thus be treated as a single baggage header.

        // options?.LogDebug("Received baggage header '{0}'.", value);

        try
        {
            return BaggageHeader.TryParse(baggageValue, onlySentry: true);
        }
        // catch (Exception ex)
        catch (Exception)
        {
            // options?.LogError("Invalid baggage header '{0}'.", ex, value);
            return null;
        }
    }
}
