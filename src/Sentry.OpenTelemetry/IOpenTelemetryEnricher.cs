namespace Sentry.OpenTelemetry;

internal interface IOpenTelemetryEnricher
{
    void Enrich(ISpanTracer span, Activity activity, IHub hub, SentryOptions? options);
}
