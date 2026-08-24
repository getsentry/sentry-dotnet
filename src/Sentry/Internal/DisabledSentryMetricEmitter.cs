namespace Sentry.Internal;

internal sealed class DisabledSentryMetricEmitter : SentryMetricEmitter
{
    internal static DisabledSentryMetricEmitter Instance { get; } = new DisabledSentryMetricEmitter();

    internal DisabledSentryMetricEmitter()
    {
    }

    /// <inheritdoc />
    private protected override void CaptureMetric<T>(SentryMetricType type, string name, T value, string? unit, IEnumerable<KeyValuePair<string, object>>? attributes, Scope? scope) where T : struct
    {
        // disabled
    }

    /// <inheritdoc />
    private protected override void CaptureMetric<T>(SentryMetricType type, string name, T value, string? unit, ReadOnlySpan<KeyValuePair<string, object>> attributes, Scope? scope) where T : struct
    {
        // disabled
    }

    /// <inheritdoc />
    private protected override void CaptureMetric<T>(SentryMetric<T> metric) where T : struct
    {
        // disabled
    }

    /// <inheritdoc />
    protected internal override void Flush()
    {
        // disabled
    }
}
