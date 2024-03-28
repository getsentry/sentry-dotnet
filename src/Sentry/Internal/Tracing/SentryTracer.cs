namespace Sentry.Internal.Tracing;

internal class SentryTracer(IHub hub) : ITracer
{
    public ITraceSpan StartSpan(string operationName) => new SentryTraceSpan(
        hub,
        hub.StartSpan(operationName, operationName)
        );

    public ITraceSpan? CurrentSpan => hub.GetSpan() is { } span
        ? new SentryTraceSpan(hub, span)
        : null;
}
