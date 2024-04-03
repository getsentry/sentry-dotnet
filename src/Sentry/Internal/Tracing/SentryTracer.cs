namespace Sentry.Internal.Tracing;

internal class SentryTracer(IHub hub) : ITracer
{
    public ITraceSpan? StartSpan(string operationName, string? description = null) => new SentryTraceSpan(
        hub,
        hub.StartSpan(operationName, description ?? operationName)
        );

    public ITraceSpan? CurrentSpan => hub.GetSpan() is {} span
        ? new SentryTraceSpan(hub, span)
        : null;
}
