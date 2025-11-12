using Sentry.Internal;

namespace Sentry.Extensions.AI;

/// <summary>
/// Listens to FunctionInvokingChatClient's Activity
/// </summary>
internal class SentryAiActivityListener
{
    private static ActivityListener? Instance;

    /// <summary>
    /// Initializes Sentry's <see cref="ActivityListener"/> to tap into FunctionInvokingChatClient's Activity
    /// </summary>
    /// <param name="hub">Optional IHub instance to use. If not provided, HubAdapter.Instance will be used.</param>
    public SentryAiActivityListener(IHub? hub = null)
    {
        if (Instance != null)
        {
            return;
        }

        var currHub = hub ?? HubAdapter.Instance;
        Instance = new ActivityListener
        {
            ShouldListenTo = source => source.Name.StartsWith(SentryAIConstants.SentryActivitySourceName),
            Sample = (ref ActivityCreationOptions<ActivityContext> options) =>
                SentryAIConstants.FICCActivityNames.Contains(options.Name)
                    ? ActivitySamplingResult.AllDataAndRecorded
                    : ActivitySamplingResult.None,
            ActivityStarted = activity =>
            {
                var agentSpan = currHub.StartSpan(SentryAIConstants.SpanAttributes.InvokeAgentOperation,
                    SentryAIConstants.SpanAttributes.InvokeAgentDescription);
                activity.SetFused(SentryAIConstants.SentryFICCSpanAttributeName, agentSpan);
            },
            ActivityStopped = activity =>
            {
                var agentSpan = activity.GetFused<ISpan>(SentryAIConstants.SentryFICCSpanAttributeName);
                // Don't pass in OK status in case there was an exception
                agentSpan?.Finish();
            }
        };
        ActivitySource.AddActivityListener(Instance);
    }

    /// <summary>
    /// Dispose the singleton instance (for testing purposes mostly)
    /// </summary>
    internal static void Dispose()
    {
        Instance?.Dispose();
        Instance = null;
    }
}
