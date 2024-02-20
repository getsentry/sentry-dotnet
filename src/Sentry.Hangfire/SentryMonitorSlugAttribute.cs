using Hangfire.Client;
using Hangfire.Common;

namespace Sentry.Hangfire;

/// <summary>
/// Sentry Monitor Slug Attribute
/// </summary>
/// <param name="monitorSlug"></param>
public class SentryMonitorSlugAttribute(string monitorSlug) : JobFilterAttribute, IClientFilter
{
    private readonly string? _monitorSlug = monitorSlug;

    /// <inheritdoc />
    public void OnCreating(CreatingContext context)
    {
        context.SetJobParameter(SentryJobFilter.SentryMonitorSlugKey, _monitorSlug);
    }

    /// <inheritdoc />
    public void OnCreated(CreatedContext context)
    { }
}
