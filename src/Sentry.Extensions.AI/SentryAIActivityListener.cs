using Sentry.Internal;

namespace Sentry.Extensions.AI;

/// <summary>
/// Listens to FunctionInvokingChatClient's Activity
/// </summary>
internal static class SentryAIActivityListener
{
    private static IHub Hub = HubAdapter.Instance;

    /// <summary>
    /// Sentry's <see cref="ActivityListener"/> to tap into function invocation's Activity
    /// </summary>
    private static readonly ActivityListener FICCListener = new()
    {
        ShouldListenTo = source => source.Name.StartsWith(SentryAIConstants.SentryActivitySourceName),
        Sample = (ref ActivityCreationOptions<ActivityContext> options) =>
            SentryAIConstants.FICCActivityNames.Contains(options.Name) ?
                ActivitySamplingResult.AllDataAndRecorded : ActivitySamplingResult.None,
        ActivityStarted = activity =>
        {
            var agentSpan = Hub.StartSpan(SentryAIConstants.SpanAttributes.InvokeAgentOperation, SentryAIConstants.SpanAttributes.InvokeAgentDescription);
            activity.SetFused(SentryAIConstants.SentryFICCSpanAttributeName, agentSpan);
        },
        ActivityStopped = activity =>
        {
            var agentSpan = activity.GetFused<ISpan>(SentryAIConstants.SentryFICCSpanAttributeName);
            // Don't pass in OK status in case there was an exception
            agentSpan?.Finish();
        }
    };

    /// <summary>
    /// Initializes Sentry's <see cref="ActivityListener"/> to tap into FunctionInvokingChatClient's Activity
    /// </summary>
    /// <param name="hub">Optional IHub instance to use. If not provided, HubAdapter.Instance will be used.</param>
    internal static void Init(IHub? hub = null)
    {
        Hub = hub ?? HubAdapter.Instance;
        ActivitySource.AddActivityListener(FICCListener);
    }
}
