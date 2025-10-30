using Sentry.Extensibility;
using Sentry.Internal;

namespace Sentry.Extensions.AI;

/// <summary>
/// Listens to FunctionInvokingChatClient's Activity
/// </summary>
internal static class SentryAIActivityListener
{
    /// <summary>
    /// Sentry's <see cref="ActivityListener"/> to tap into function invocation's Activity
    /// </summary>
    private static readonly ActivityListener FICCListener = new()
    {
        ShouldListenTo = s => s.Name.StartsWith(SentryAIConstants.SentryActivitySourceName),
        Sample = (ref ActivityCreationOptions<ActivityContext> options) =>
            options.Name == SentryAIConstants.FICCActivityName ?
                ActivitySamplingResult.AllDataAndRecorded : ActivitySamplingResult.None,
        ActivityStarted = a =>
        {
            var currSpan = HubAdapter.Instance.StartSpan(SentryAIConstants.SpanAttributes.InvokeAgentOperation, SentryAIConstants.SpanAttributes.InvokeAgentDescription);
            a.SetFused(SentryAIConstants.SentryActivitySpanAttributeName, currSpan);
        },
        ActivityStopped = a =>
        {
            var currSpan = a.GetFused<ISpan>(SentryAIConstants.SentryActivitySpanAttributeName);
            currSpan?.Finish(SpanStatus.Ok);
        },
    };

    /// <summary>
    /// Initializes Sentry's <see cref="ActivityListener"/> to tap into FunctionInvokingChatClient's Activity
    /// </summary>
    internal static void Init()
    {
        ActivitySource.AddActivityListener(FICCListener);
    }
}
