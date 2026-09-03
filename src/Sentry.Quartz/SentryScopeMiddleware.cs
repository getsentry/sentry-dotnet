using Quartz;

namespace Sentry.Quartz;

internal sealed class SentryScopeMiddleware(IHub sentryHub) : IJobExecutionMiddleware
{
    private readonly IHub _sentryHub = sentryHub;

    public async ValueTask Invoke(IJobExecutionContext context, JobExecutionDelegate next, CancellationToken cancellationToken)
    {
        using var _ = _sentryHub.PushScope();
        await next(context, cancellationToken).ConfigureAwait(false);
    }
}
