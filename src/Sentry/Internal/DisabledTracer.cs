namespace Sentry.Internal;

internal class DisabledTracer : ISentryTracer
{
    private static readonly Lazy<DisabledTracer> LazyInstance = new();
    public static DisabledTracer Instance => LazyInstance.Value;
    public ISentrySpan StartSpan(string operationName) => DisabledSpan.Instance;
    public ISentrySpan? CurrentSpan => DisabledSpan.Instance;
}
