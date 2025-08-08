using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

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
            var hub = serviceProvider.GetRequiredService<IHub>();
            return new SentryChatClient(inner, hub, agentName, model, system);
        });
    }

    /// <summary>
    /// Wraps an IChatClient with Sentry instrumentation.
    /// </summary>
    public static IChatClient WithSentry(this IChatClient client, IHub hub, string? agentName = null, string? model = null, string? system = null)
    {
        return new SentryChatClient(client, hub, agentName, model, system);
    }
}


