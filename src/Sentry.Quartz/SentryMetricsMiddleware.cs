using Quartz;

namespace Sentry.Quartz;

internal sealed class SentryMetricsMiddleware : IJobExecutionMiddleware
{
    private readonly IHub _sentryHub;
    private readonly SentryMetricsOptions _options;

    public SentryMetricsMiddleware(IHub sentryHub, SentryMetricsOptions options)
    {
        _sentryHub = sentryHub;
        _options = options;
    }

    public async ValueTask Invoke(IJobExecutionContext context, JobExecutionDelegate next, CancellationToken cancellationToken)
    {
        using (GetLogger(context))
        {
            await next(context, cancellationToken).ConfigureAwait(false);
        }
    }

    private SentryMetricsLogger? GetLogger(IJobExecutionContext context)
    {
        if (!context.JobDetail.JobDataMap.TryGetBoolean("LogMetrics", out bool logMetrics) || logMetrics)
        {
            var metricsName = _options.ResolveMetricsName(context.JobDetail);
            var additionalAttributes = _options.AdditionalAttributes?.Invoke(context.JobDetail);
            return new SentryMetricsLogger(metricsName, additionalAttributes, _sentryHub);
        }

        return null;
    }
}

public class SentryMetricsOptions
{
    public Func<IJobDetail, string> ResolveMetricsName { get; set; } = jobDetail => $"quartz.job.duration.{jobDetail.Key.Group}.{jobDetail.Key.Name}";

    public Func<IJobDetail, IDictionary<string, object>>? AdditionalAttributes { get; set; }
}

internal sealed class SentryMetricsLogger : IDisposable
{
    private readonly string _metricName;
    private readonly IHub _sentryHub;
    private readonly Stopwatch _stopWatch = Stopwatch.StartNew();
    private readonly IDictionary<string, object>? _attributes;

    public SentryMetricsLogger(string metricName, IDictionary<string, object>? attributes, IHub sentryHub)
    {
        _attributes = attributes;
        _metricName = metricName;
        _sentryHub = sentryHub;
    }

    public void Dispose()
    {
        _sentryHub.Metrics.EmitDistribution(_metricName, _stopWatch.ElapsedMilliseconds, MeasurementUnit.Duration.Millisecond, _attributes);
    }
}
