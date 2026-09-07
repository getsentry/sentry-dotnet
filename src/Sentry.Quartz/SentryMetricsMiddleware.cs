using Microsoft.Extensions.Options;
using Quartz;

namespace Sentry.Quartz;

internal sealed class SentryMetricsMiddleware : IJobExecutionMiddleware
{
    private readonly IHub _sentryHub;
    private readonly IOptions<SentryMetricsOptions> _options;

    public SentryMetricsMiddleware(IHub sentryHub, IOptions<SentryMetricsOptions> options)
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
            var metricsName = _options.Value.ResolveMetricsName(context.JobDetail);
            var additionalAttributes = _options.Value.AdditionalAttributes?.Invoke(context.JobDetail);
            return new SentryMetricsLogger(metricsName, additionalAttributes, _sentryHub);
        }

        return null;
    }
}

/// <summary>
/// Options used by <see cref="SentryMetricsMiddleware"/> to control how Quartz job execution metrics are
/// reported to Sentry.
/// </summary>
public class SentryMetricsOptions
{
    /// <summary>
    /// A function used to resolve the name of the metric emitted for a job's execution duration.
    /// </summary>
    /// <remarks>
    /// Defaults to <c>quartz.job.duration.{group}.{name}</c>, based on the job's <see cref="IJobDetail.Key"/>.
    /// </remarks>
    public Func<IJobDetail, string> ResolveMetricsName { get; set; } = jobDetail => $"quartz.job.duration.{jobDetail.Key.Group}.{jobDetail.Key.Name}";

    /// <summary>
    /// An optional function used to compute additional attributes to attach to the emitted duration metric,
    /// based on the job being executed.
    /// <remarks>
    /// Returns <see langword="null"/> by default, meaning no additional
    /// attributes are attached.</remarks>
    /// </summary>
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
