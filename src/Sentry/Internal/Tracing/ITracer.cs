namespace Sentry.Internal.Tracing;

internal interface ITracer
{
    ITraceSpan? StartSpan(string operationName, string? description = null);
    ITraceSpan? CurrentSpan { get; }
}
