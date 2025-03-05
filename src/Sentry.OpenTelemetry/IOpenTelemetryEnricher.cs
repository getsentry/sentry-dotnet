namespace Sentry.OpenTelemetry;

internal interface IOpenTelemetryEnricher
{
    public void Enrich(ISpan span, Activity activity, IHub hub, SentryOptions? options);
}
