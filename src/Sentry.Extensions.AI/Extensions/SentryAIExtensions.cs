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
    /// <param name="options">The <see cref="ChatOptions"/> that contains the <see cref="AIFunction"/> to instrument</param>
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
        }

        return options;
    }

    /// <summary>
    /// Wraps an IChatClient with Sentry agent instrumentation.
    /// </summary>
    /// <remarks>
    /// This method can be used either with an existing Sentry setup or as a standalone integration.
    /// If Sentry is already initialized, it will use the existing configuration.
    /// If not, it will initialize Sentry with the provided options.
    /// </remarks>
    /// <param name="client">The <see cref="IChatClient"/> to be instrumented</param>
    /// <param name="configure">The <see cref="SentryAIOptions"/> configuration</param>
    /// <returns>The instrumented <see cref="IChatClient"/></returns>
    public static IChatClient WithSentry(this IChatClient client, Action<SentryAIOptions>? configure = null)
    {
        return new SentryChatClient(client, configure);
    }
}
