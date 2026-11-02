using Quartz;

namespace Sentry.Quartz;

// The Quartz 4 side of the APIs that differ between Quartz 3 and 4
internal static class QuartzApi
{
    // Quartz 4 reads '*' and '?' alike in the day fields, and runs on either day when both restrict
    public static bool UnionsDayFields => true;

    public static Type GetJobType(IJobDetail jobDetail) => jobDetail.JobType.Type;

    // Returns the triggers of every job that matches the predicate
    public static async Task<List<(IJobDetail Job, ITrigger Trigger)>> GetTriggersAsync(
        IScheduler scheduler,
        Func<IJobDetail, bool> predicate,
        CancellationToken cancellationToken)
    {
        var headers = await scheduler.QueryTriggers(new TriggerQuery { Take = PagedQuery.All }, cancellationToken)
            .ConfigureAwait(false);
        var jobKeys = headers.Items.Select(header => header.JobKey).Distinct().ToList();
        var jobs = (await scheduler.GetJobDetails(jobKeys, cancellationToken).ConfigureAwait(false))
            .Where(predicate)
            .ToDictionary(job => job.Key);

        var triggerKeys = headers.Items.Where(header => jobs.ContainsKey(header.JobKey)).Select(header => header.Key).ToList();
        if (triggerKeys.Count == 0)
        {
            return [];
        }

        var result = new List<(IJobDetail Job, ITrigger Trigger)>();
        foreach (var trigger in await scheduler.GetTriggers(triggerKeys, cancellationToken).ConfigureAwait(false))
        {
            if (jobs.TryGetValue(trigger.JobKey, out var job))
            {
                result.Add((job, trigger));
            }
        }

        return result;
    }
}
