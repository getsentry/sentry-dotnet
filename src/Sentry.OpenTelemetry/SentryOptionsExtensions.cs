namespace Sentry.OpenTelemetry;

/// <summary>
/// OpenTelemetry Extensions for <see cref="SentryOptions"/>.
/// </summary>
public static class SentryOptionsExtensions
{
    /// <summary>
    /// Enables OpenTelemetry instrumentation with Sentry
    /// </summary>
    /// <param name="options"></param>
    public static void UseOpenTelemetry(this SentryOptions options)
    {
        options.Instrumenter = Instrumenter.OpenTelemetry;
        options.ImplicitTransactionProcessors.Add(
            new OpenTelemetryTransactionProcessor()
            );
    }

    internal static bool IsSentryRequest(this SentryOptions options, string? requestUri)=>
        !string.IsNullOrEmpty(requestUri) && options.IsSentryRequest(new Uri(requestUri));

    internal static bool IsSentryRequest(this SentryOptions options, Uri? requestUri)
    {
        if (string.IsNullOrEmpty(options.Dsn) || requestUri is null)
        {
            return false;
        }

        var requestBaseUrl = requestUri.GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped);
        return string.Equals(requestBaseUrl, options.SentryBaseUrl, StringComparison.OrdinalIgnoreCase);
    }
}
