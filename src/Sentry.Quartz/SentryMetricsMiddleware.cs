using Quartz;

namespace Sentry.Quartz;

internal sealed class SentryMetricsMiddleware(IHub sentryHub) : IJobExecutionMiddleware
{
    private readonly IHub _sentryHub = sentryHub;

    public async ValueTask Invoke(IJobExecutionContext context, JobExecutionDelegate next, CancellationToken cancellationToken)
    {
        using var _ = _sentryHub.PushScope();

        var stopWatch = Stopwatch.StartNew();
        await next(context, cancellationToken).ConfigureAwait(false);
        stopWatch.Stop();

        var attributes = new Dictionary<string, object>
        {
            { "payload.job.name", context.JobInstance.GetType().Name },
        };

        _sentryHub.Metrics.EmitDistribution("quartz.job.duration", stopWatch.ElapsedMilliseconds, MeasurementUnit.Duration.Millisecond, attributes);
    }
}
