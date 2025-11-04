using Sentry.Internal;

namespace Sentry.Extensions.AI;

internal static class SentryAIUtil
{
    internal static ISpan? GetActivitySpan()
    {
        var currActivity = Activity.Current;
        while (currActivity != null)
        {
            if (currActivity.GetFused<ISpan>(SentryAIConstants.SentryActivitySpanAttributeName) is { } span)
            {
                return span;
            }

            currActivity = currActivity.Parent;
        }

        return null;
    }
}
