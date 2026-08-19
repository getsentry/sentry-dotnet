// Alias required because Android TFMs of the core Sentry package otherwise see an ambiguous reference
// between System.Diagnostics.Activity (global using) and Android.App.Activity (Android implicit usings).
using Activity = System.Diagnostics.Activity;

namespace Sentry.Internal.Tracing;

/// <summary>
/// Enriches Sentry spans with additional information from the <see cref="Activity"/> that produced them,
/// just before the span is finished. Core equivalent of Sentry.OpenTelemetry.IOpenTelemetryEnricher.
/// </summary>
internal interface ISentryActivityEnricher
{
    public void Enrich(ISpan span, Activity activity, IHub hub, SentryOptions? options);
}
