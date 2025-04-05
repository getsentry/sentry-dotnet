namespace Sentry;

/// <summary>
/// Extension methods for working with Sentry trace headers.
/// </summary>
internal static class SentryTraceHeaderExtensions
{
    /// <summary>
    /// Converts the Sentry trace header to W3C trace context format.
    /// </summary>
    /// <param name="traceHeader">The Sentry trace header to convert.</param>
    /// <returns>A string representation of the trace header in W3C format.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="traceHeader"/> is null.</exception>
    public static string AsW3CTraceContext(this SentryTraceHeader traceHeader)
    {
        if (traceHeader is null)
        {
            throw new ArgumentNullException(nameof(traceHeader));
        }

        const string version = "00";
        var traceFlags = ConvertSampledToTraceFlags(traceHeader.IsSampled);
        if (traceFlags is null)
        {
            return $"{version}-{traceHeader.TraceId}-{traceHeader.SpanId}";
        }

        return $"{version}-{traceHeader.TraceId}-{traceHeader.SpanId}-{traceFlags}";
    }

    private static string? ConvertSampledToTraceFlags(bool? isSampled)
    {
        return isSampled switch
        {
            true => "01",
            false => "00",
            null => null
        };
    }
}
