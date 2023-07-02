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

    internal static bool IsSentryRequest(this SentryOptions options, string requestUri)
        => !string.IsNullOrEmpty(options.Dsn) && requestUri.StartsWith(options.Dsn!, StringComparison.OrdinalIgnoreCase);

    internal static bool IsSentryRequest(this SentryOptions options, Uri? requestUri)
        => IsSentryRequest(options, requestUri?.ToString() ?? string.Empty);
}
