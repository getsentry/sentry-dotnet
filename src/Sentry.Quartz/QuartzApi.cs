using Quartz;

namespace Sentry.Quartz;

// The Quartz 4 side of the APIs that differ between Quartz 3 and 4
internal static class QuartzApi
{
    // Quartz 4 reads '*' and '?' alike in the day fields, and runs on either day when both restrict
    public static bool UnionsDayFields => true;

    public static Type GetJobType(IJobDetail jobDetail) => jobDetail.JobType.Type;

    public static async Task<bool> HasSeveralTriggersAsync(IJobExecutionContext context, CancellationToken cancellationToken)
    {
        var query = new TriggerQuery { Job = context.JobDetail.Key, Take = 2 };
        var triggers = await context.Scheduler.QueryTriggers(query, cancellationToken).ConfigureAwait(false);
        return triggers.Items.Count > 1;
    }
}
