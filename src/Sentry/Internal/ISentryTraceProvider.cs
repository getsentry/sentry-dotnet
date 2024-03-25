namespace Sentry.Internal;

internal interface ISentryTraceProvider
{
    public ISentryTracer GetTracer(string name, string? version = "");
}
