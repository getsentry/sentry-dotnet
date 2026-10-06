using Quartz;

namespace Sentry.Quartz;

internal sealed class SentryCronJobMiddleware(JobMonitor monitor) : IJobExecutionMiddleware
{
    public async ValueTask Invoke(IJobExecutionContext context, JobExecutionDelegate next, CancellationToken cancellationToken)
    {
        var checkIn = await monitor.StartAsync(context, cancellationToken).ConfigureAwait(false);
        if (checkIn is null)
        {
            await next(context, cancellationToken).ConfigureAwait(false);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            await next(context, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            monitor.Finish(checkIn, failed: true, stopwatch.Elapsed);
            throw;
        }

        // A job can stop early when cancelled without throwing
        monitor.Finish(checkIn, failed: cancellationToken.IsCancellationRequested, stopwatch.Elapsed);
    }
}
