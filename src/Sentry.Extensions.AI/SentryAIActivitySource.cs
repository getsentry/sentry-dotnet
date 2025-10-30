using Microsoft.Extensions.AI;

namespace Sentry.Extensions.AI;

/// <summary>Sentry's <see cref="ActivitySource"/> to be used in <see cref="IChatClient"/></summary>
internal static class SentryAIActivitySource
{
    /// <summary>Sentry's <see cref="ActivitySource"/> to be used in <see cref="IChatClient"/></summary>
    internal static ActivitySource Instance { get; } = new(SentryAIConstants.SentryActivitySourceName);
}
