using Quartz;

namespace Sentry.Quartz;

// The Quartz 3 side of the APIs that differ between Quartz 3 and 4
internal static class QuartzApi
{
    // Quartz 3 needs exactly one day field to be '?', and the other decides
    public static bool UnionsDayFields => false;

    public static Type GetJobType(IJobDetail jobDetail) => jobDetail.JobType;

    public static async Task<bool> HasSeveralTriggersAsync(IJobExecutionContext context, CancellationToken cancellationToken)
    {
        var triggers = await context.Scheduler.GetTriggersOfJob(context.JobDetail.Key, cancellationToken).ConfigureAwait(false);
        return triggers.Count > 1;
    }
}
