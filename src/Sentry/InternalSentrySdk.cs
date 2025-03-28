namespace Sentry;

/// <summary>
/// Sentry SDK internal API methods meant for being used by the Sentry Unity SDK
/// </summary>
public static class InternalSentrySdk
{
    /// <summary>
    /// Allows to set the trace
    /// </summary>
    public static void SetTrace(SentryId traceId, SpanId parentSpanId) =>
        SentrySdk.CurrentHub.ConfigureScope(scope =>
            scope.SetPropagationContext(new SentryPropagationContext(traceId, parentSpanId)));
}
