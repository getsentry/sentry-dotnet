using Microsoft.Extensions.AI;
using Sentry.Extensibility;

// ReSharper disable once CheckNamespace -- Discoverability
namespace Sentry.Extensions.AI;

/// <summary>
/// Extensions to instrument Microsoft.Extensions.AI builders with Sentry
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class SentryAIExtensions
{
    /// <summary>
    /// Wrap tool calls specified in <see cref="ChatOptions"/> with Sentry agent instrumentation
    /// </summary>
    public static ChatOptions WithSentry(
        this ChatOptions options)
    {
        if (options.Tools is null || options.Tools.Count == 0)
        {
            return options;
        }

        for (var i = 0; i < options.Tools.Count; i++)
        {
            var tool = options.Tools[i];
            if (tool is AIFunction fn and not SentryInstrumentedFunction)
            {
                options.Tools[i] = new SentryInstrumentedFunction(fn);
            }
            else
            {
                options.Tools[i] = tool;
            }
        }

        return options;
    }

    /// <summary>
    /// Wraps an IChatClient with Sentry instrumentation.
    /// </summary>
    public static IChatClient WithSentry(this IChatClient client)
    {
        return new SentryChatClient(client, HubAdapter.Instance);
    }
}
