using Sentry.Internal;

namespace Sentry.Extensions.AI;

/// <summary>
/// Listens to FunctionInvokingChatClient's Activity
/// </summary>
internal static class SentryAIActivityListener
{
    /// <summary>
    /// Singleton used outside of testing
    /// </summary>
    private static readonly Lazy<ActivityListener> LazyInstance = new(() => CreateListener());
    internal static readonly ActivityListener Instance = LazyInstance.Value;

    /// <summary>
    /// Initializes Sentry's <see cref="ActivityListener"/> to tap into FunctionInvokingChatClient's Activity
    /// </summary>
    public static ActivityListener CreateListener(IHub? hub = null)
    {
        hub ??= HubAdapter.Instance;

        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name.StartsWith(SentryAIActivitySource.SentryActivitySourceName),
            Sample = (ref ActivityCreationOptions<ActivityContext> options) =>
                SentryAIConstants.FICCActivityNames.Contains(options.Name)
                    ? ActivitySamplingResult.AllDataAndRecorded
                    : ActivitySamplingResult.None,
            ActivityStarted = activity =>
            {
                var agentSpan = hub.StartSpan(SentryAIConstants.SpanAttributes.InvokeAgentOperation,
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
        ActivitySource.AddActivityListener(listener);
        return listener;
    }
}
