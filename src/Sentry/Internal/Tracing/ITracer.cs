namespace Sentry.Internal.Tracing;

internal interface ITracer
{
    ITraceSpan? StartSpan(string operationName);
    ITraceSpan? CurrentSpan { get; }
}
