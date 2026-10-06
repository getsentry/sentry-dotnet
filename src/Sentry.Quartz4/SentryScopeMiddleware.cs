using Quartz;

namespace Sentry.Quartz;

// Runs each job in its own scope and trace, and captures what the job throws inside it
internal sealed class SentryScopeMiddleware(IHub hub) : IJobExecutionMiddleware
{
    internal const string JobTag = "quartz.job";
    internal const string MechanismType = "Quartz";

    public async ValueTask Invoke(IJobExecutionContext context, JobExecutionDelegate next, CancellationToken cancellationToken)
    {
        using var _ = hub.PushScope();
        hub.ConfigureScope(static (scope, context) =>
        {
            scope.SetPropagationContext(new SentryPropagationContext());
            scope.SetTag(JobTag, context.JobDetail.Key.ToString());
        }, context);

        try
        {
            await next(context, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e) when (e is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            // Quartz catches it, so it doesn't end the process
            e.SetSentryMechanism(MechanismType, handled: false, terminal: false);
            hub.CaptureException(e);
            throw;
        }
    }
}
