using Sentry.Internal;

namespace Sentry.Extensions.AI;

internal static class SentryAIUtil
{
    internal static Activity? GetFICCActivity()
    {
        var currActivity = Activity.Current;
        while (currActivity != null)
        {
            if (currActivity.GetFused<ISpan>(SentryAIConstants.SentryFICCSpanAttributeName) is not null)
            {
                return currActivity;
            }

            currActivity = currActivity.Parent;
        }

        return null;
    }

    internal static ISpan? GetFICCSpan(this Activity ficcActivity) =>
        ficcActivity.GetFused<ISpan>(SentryAIConstants.SentryFICCSpanAttributeName);

    internal static void SetAIOptions(this Activity ficcActivity, SentryAIOptions options) =>
        ficcActivity.SetFused(options);

    internal static SentryAIOptions? GetAIOptions(this Activity ficcActivity) =>
        ficcActivity.GetFused<SentryAIOptions>();
}
