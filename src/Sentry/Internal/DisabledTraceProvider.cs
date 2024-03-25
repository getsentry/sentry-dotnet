namespace Sentry.Internal;

internal class DisabledTraceProvider : ISentryTraceProvider
{
    private static readonly Lazy<DisabledTraceProvider> LazyInstance = new();
    public static DisabledTraceProvider Instance => LazyInstance.Value;
    public ISentryTracer GetTracer(string name, string? version = "") => DisabledTracer.Instance;
}
