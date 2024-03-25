namespace Sentry.Internal;

internal interface ISentryTracer
{
    ISentrySpan? StartSpan(string operationName);
    ISentrySpan? CurrentSpan { get; }
}
