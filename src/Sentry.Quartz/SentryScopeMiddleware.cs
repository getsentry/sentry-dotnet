using Quartz;

namespace Sentry.Quartz;

// Runs each job in its own scope and trace
internal sealed class SentryScopeMiddleware(IHub hub, SentryOptions? options = null) : IJobExecutionMiddleware
{
    internal const string JobTag = "quartz.job";

    public async ValueTask Invoke(IJobExecutionContext context, JobExecutionDelegate next, CancellationToken cancellationToken)
    {
        // Jobs share the one scope in global mode, so leave it alone
        if ((options ?? hub.GetSentryOptions())?.IsGlobalModeEnabled is true)
        {
            await next(context, cancellationToken).ConfigureAwait(false);
            return;
        }

        using var _ = hub.PushScope();
        hub.ConfigureScope(static (scope, context) =>
        {
            scope.SetPropagationContext(new SentryPropagationContext());
            scope.SetTag(JobTag, context.JobDetail.Key.ToString());
        }, context);

        await next(context, cancellationToken).ConfigureAwait(false);
    }
}
