using Hangfire;
using Hangfire.Client;
using Hangfire.Common;
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

/// <summary>
/// Sentry's Hangfire Helper
/// </summary>
public static class SentryHangfire
{
    /// <summary>
    /// The monitor slug Sentry is to associate with the job
    /// </summary>
    public static string SentryMonitorSlugKey = "SentryMonitorSlug";
}

/// <summary>
/// Sentry Monitor Slug Attribute
/// </summary>
/// <param name="monitorSlug"></param>
public class SentryMonitorSlugAttribute(string monitorSlug) : JobFilterAttribute, IClientFilter
{
    /// <summary>
    /// Monitor Slug
    /// </summary>
    private string? MonitorSlug { get; } = monitorSlug;

    /// <inheritdoc />
    public void OnCreating(CreatingContext context)
    {
        context.SetJobParameter(SentryHangfire.SentryMonitorSlugKey, MonitorSlug);
    }

    /// <inheritdoc />
    public void OnCreated(CreatedContext context)
    { }
}

internal class SentryJobFilter : IServerFilter
{
    private const string SentryCheckInIdKey = "SentryCheckInIdKey";

    public void OnPerforming(PerformingContext context)
    {
        var monitorSlug = context.GetJobParameter<string>(SentryHangfire.SentryMonitorSlugKey);
        if (monitorSlug is null)
        {
            return;
        }

        var checkInId = SentrySdk.CaptureCheckIn(new SentryCheckIn(monitorSlug, CheckinStatus.InProgress));
        context.SetJobParameter(SentryCheckInIdKey, checkInId);
    }

    public void OnPerformed(PerformedContext context)
    {
        var monitorSlug = context.GetJobParameter<string>(SentryHangfire.SentryMonitorSlugKey);
        if (monitorSlug is null)
        {
            return;
        }

        var checkInId = context.GetJobParameter<SentryId?>(SentryCheckInIdKey);
        if (checkInId is null)
        {
            return;
        }

        var status = CheckinStatus.Ok;
        if (context.Exception is null)
        {
            status = CheckinStatus.Error;
        }

        _ = SentrySdk.CaptureCheckIn(new SentryCheckIn(monitorSlug, status, checkInId));
    }
}
