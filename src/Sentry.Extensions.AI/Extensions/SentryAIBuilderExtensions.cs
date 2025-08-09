using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Sentry.Extensibility;

namespace Sentry.Extensions.AI;

/// <summary>
/// Extensions to instrument Microsoft.Extensions.AI builders with Sentry
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class SentryAIExtensions
{
    /// <summary>
    /// Adds Sentry instrumentation to the ChatClientBuilder pipeline.
    /// </summary>
    public static ChatClientBuilder UseSentry(this ChatClientBuilder builder, string? agentName = null, string? model = null, string? system = null)
    {
        return builder.Use((serviceProvider, inner) =>
        {
            // Try to get IHub from DI first, fallback to HubAdapter.Instance
            var hub = serviceProvider.GetService<IHub>() ?? HubAdapter.Instance;
            return new SentryChatClient(inner, hub, agentName, model, system);
        });
    }

    /// <summary>
    /// Wraps an IChatClient with Sentry instrumentation.
    /// </summary>
    public static IChatClient WithSentry(this IChatClient client, string? agentName = null, string? model = null, string? system = null)
    {
        return new SentryChatClient(client, HubAdapter.Instance, agentName, model, system);
    }
}


