using Quartz;
using Quartz.Impl.Matchers;

namespace Sentry.Quartz;

// The Quartz 3 side of the APIs that differ between Quartz 3 and 4
internal static class QuartzApi
{
    // Quartz 3 needs exactly one day field to be '?', and the other decides
    public static bool UnionsDayFields => false;

    public static Type GetJobType(IJobDetail jobDetail) => jobDetail.JobType;

    // Returns the triggers of every job that matches the predicate. Quartz 3 has no bulk lookups, so this reads each
    // job, and the triggers of each matching one.
    public static async Task<List<(IJobDetail Job, ITrigger Trigger)>> GetTriggersAsync(
        IScheduler scheduler,
        Func<IJobDetail, bool> predicate,
        CancellationToken cancellationToken)
    {
        var result = new List<(IJobDetail Job, ITrigger Trigger)>();
        var jobKeys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup(), cancellationToken).ConfigureAwait(false);
        foreach (var jobKey in jobKeys)
        {
            // Null when the job was deleted after its key was read
            var job = await scheduler.GetJobDetail(jobKey, cancellationToken).ConfigureAwait(false);
            if (job is null || !predicate(job))
            {
                continue;
            }

            foreach (var trigger in await scheduler.GetTriggersOfJob(jobKey, cancellationToken).ConfigureAwait(false))
            {
                result.Add((job, trigger));
            }
        }

        return result;
    }
}
