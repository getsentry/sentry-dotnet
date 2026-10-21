using Quartz;

namespace Sentry.Quartz;

// Runs each job in its own scope and trace, and captures what the job throws inside it
internal sealed class SentryScopeMiddleware(IHub hub, SentryOptions? options = null) : IJobExecutionMiddleware
{
    internal const string JobTag = "quartz.job";
    internal const string MechanismType = "Quartz";

    // The exceptions captured here, so the log entry Quartz writes for them can be dropped
    private static readonly ConditionalWeakTable<Exception, object?> CapturedExceptions = new();

    internal static bool WasCaptured(Exception exception) => CapturedExceptions.TryGetValue(exception, out _);

    public async ValueTask Invoke(IJobExecutionContext context, JobExecutionDelegate next, CancellationToken cancellationToken)
    {
        // Jobs share the one scope in global mode, so leave it alone
        if ((options ?? hub.GetSentryOptions())?.IsGlobalModeEnabled is true)
        {
            await InvokeAndCapture(context, next, cancellationToken).ConfigureAwait(false);
            return;
        }

        using var _ = hub.PushScope();
        hub.ConfigureScope(static (scope, context) =>
        {
            scope.SetPropagationContext(new SentryPropagationContext());
            scope.SetTag(JobTag, context.JobDetail.Key.ToString());
        }, context);

        await InvokeAndCapture(context, next, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask InvokeAndCapture(IJobExecutionContext context, JobExecutionDelegate next, CancellationToken cancellationToken)
    {
        try
        {
            await next(context, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e) when (e is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            e.SetSentryMechanism(MechanismType, handled: false, terminal: false);
            hub.CaptureException(e);
            CapturedExceptions.AddOrUpdate(e, null);
            throw;
        }
    }
}
