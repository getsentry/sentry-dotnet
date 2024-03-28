namespace Sentry;

/// <summary>
/// Describes which approach is used to create spans.
/// </summary>
public enum Instrumenter
{
    /// <summary>
    /// Spans are instrumented via the Sentry SDK.
    /// </summary>
    Sentry,

    /// <summary>
    /// Spans are instrumented with Sentry using ActivitySource.
    /// </summary>
    ActivitySource,

    /// <summary>
    /// Spans are instrumented via OpenTelemetry.
    /// </summary>
    OpenTelemetry
}
