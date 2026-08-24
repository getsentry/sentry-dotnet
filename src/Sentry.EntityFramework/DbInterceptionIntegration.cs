namespace Sentry.EntityFramework;

internal class DbInterceptionIntegration : ISdkIntegration, ISentryTracingIntegration
{
    // Internal for testing.
    internal IDbInterceptor? SqlInterceptor { get; private set; }

    internal const string SampleRateDisabledMessage = "EF performance won't be collected because TracesSampleRate is set to 0.";

    public void Register(IHub hub, SentryOptions options)
    {
        if (!options.IsPerformanceMonitoringEnabled)
        {
            options.DiagnosticLogger?.LogInfo(SampleRateDisabledMessage);
        }
        else
        {
            SqlInterceptor = new SentryQueryPerformanceListener(hub, options);
            DbInterception.Add(SqlInterceptor);
        }
    }

    public void Unregister()
    {
        if (SqlInterceptor is { } interceptor)
        {
            DbInterception.Remove(interceptor);
        }
    }
}
