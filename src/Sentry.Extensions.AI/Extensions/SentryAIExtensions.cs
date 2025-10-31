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
        SentryAIActivityListener.Init();
        return new SentryChatClient(client, configure);
    }
}
