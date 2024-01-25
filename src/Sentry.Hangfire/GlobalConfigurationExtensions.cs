using Hangfire;
using Hangfire.Server;

namespace Sentry.Hangfire;

/// <summary>
/// Hangfire Extensions for <see cref="SentryOptions"/>.
/// </summary>
public static class GlobalConfigurationExtensions
{
    /// <summary>
    /// Uses Sentry
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static IGlobalConfiguration UseSentry(this IGlobalConfiguration configuration)
    {
        configuration.UseFilter(new SentryJobFilter());
        return configuration;
    }
}

internal class SentryJobFilter : IServerFilter
{
    public void OnPerforming(PerformingContext context)
    {
        // Checkin: In Progress
    }

    public void OnPerformed(PerformedContext context)
    {
        // Checkin: Finished (or not)

        // context.Result
        // context.Canceled
        // context.Exception
    }
}
