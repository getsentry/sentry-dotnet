namespace Sentry.Internal.Tracing;

/// <summary>
/// Enriches Sentry spans with additional information from the <see cref="Activity"/> that produced them,
/// just before the span is finished. Core equivalent of Sentry.OpenTelemetry.IOpenTelemetryEnricher.
/// </summary>
internal interface ISentryActivityEnricher
{
    public void Enrich(ISpan span, Activity activity, IHub hub, SentryOptions? options);
}
