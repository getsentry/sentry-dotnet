namespace Sentry.OpenTelemetry;

internal interface IOpenTelemetryEnricher
{
    void Enrich(ISpan span, Activity activity, IHub hub, SentryOptions? options);
}
