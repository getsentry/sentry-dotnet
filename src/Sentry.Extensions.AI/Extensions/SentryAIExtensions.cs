using Sentry.Extensions.AI;
using Sentry.Infrastructure;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Extensions to instrument Microsoft.Extensions.AI builders with Sentry
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class SentryAIExtensions
{
    /// <summary>
    /// Wrap tool calls specified in <see cref="ChatOptions"/> with Sentry agent instrumentation
    /// </summary>
    /// <remarks>
    /// This API is experimental, and it may change in the future.
    /// </remarks>
    /// <param name="options">The <see cref="ChatOptions"/> that contains the <see cref="AIFunction"/> to instrument</param>
    [Experimental(DiagnosticId.ExperimentalFeature)]
    public static ChatOptions AddSentryToolInstrumentation(this ChatOptions options)
    {
        if (options.Tools is not { Count: > 0 })
        {
            return options;
        }

        for (var i = 0; i < options.Tools?.Count; i++)
        {
            if (options.Tools[i] is AIFunction fn and not SentryInstrumentedFunction)
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
    /// <para>
    /// This method has to be used with an existing Sentry setup. You need to initialize SentrySDK explicitly for
    /// AI Agent monitoring to work properly.
    /// </para>
    /// <para>
    /// This API is experimental, and it may change in the future.
    /// </para>
    /// </remarks>
    /// <param name="client">The <see cref="IChatClient"/> to be instrumented</param>
    /// <param name="configure">The <see cref="SentryAIOptions"/> configuration</param>
    /// <returns>The instrumented <see cref="IChatClient"/></returns>
    [Experimental(DiagnosticId.ExperimentalFeature)]
    public static IChatClient AddSentry(this IChatClient client, Action<SentryAIOptions>? configure = null) =>
        AddSentry(client, SentryAiActivityListener.Instance, configure);

    /// <summary>
    /// Internal overload for testing
    /// </summary>
    internal static IChatClient AddSentry(this IChatClient client, ActivityListener listener, Action<SentryAIOptions>? configure = null)
    {
        ActivitySource.AddActivityListener(listener);
        return new SentryChatClient(client, configure);
    }
}
