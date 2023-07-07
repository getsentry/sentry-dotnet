namespace Sentry.OpenTelemetry;

/// <summary>
/// OpenTelemetry Extensions for <see cref="SentryOptions"/>.
/// </summary>
public static class SentryOptionsExtensions
{
    /// <summary>
    /// Enables OpenTelemetry instrumentation with Sentry
    /// </summary>
    /// <param name="options"><see cref="SentryOptions"/> instance</param>
    public static void UseOpenTelemetry(this SentryOptions options)
    {
        options.Instrumenter = Instrumenter.OpenTelemetry;
        options.AddTransactionProcessor(
            new OpenTelemetryTransactionProcessor()
            );
    }
}
