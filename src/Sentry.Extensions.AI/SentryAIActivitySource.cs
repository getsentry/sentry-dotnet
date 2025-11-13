using Microsoft.Extensions.AI;

namespace Sentry.Extensions.AI;

/// <summary>Sentry's <see cref="ActivitySource"/> to be used in <see cref="IChatClient"/></summary>
internal static class SentryAIActivitySource
{
    internal const string SentryActivitySourceName = "Sentry.AgentMonitoring";

    private static readonly Lazy<ActivitySource> LazyInstance = new(CreateSource);
    internal static ActivitySource Instance => LazyInstance.Value;

    internal static ActivitySource CreateSource() => new(SentryActivitySourceName);
}
