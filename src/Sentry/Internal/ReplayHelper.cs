#if __ANDROID__
using Sentry.Android.Extensions;
#endif

namespace Sentry.Internal;

internal static class ReplayHelper
{
    /// <summary>
    /// Function that resolves the replay ID - for use in tests only.
    /// </summary>
    internal static Func<SentryId?>? TestReplayIdResolver;
    internal static Lazy<SentryId?> TestReplayId = new(() => SentryId.Create());

    internal static SentryId? GetReplayId()
    {
        if (TestReplayIdResolver is {} resolver)
        {
            // This is a test, so we need to return a test ID
            return resolver();
        }
        return ConcreteReplayIdResolver();
    }

    private static SentryId? ConcreteReplayIdResolver()
    {
#if __ANDROID__
        // Check to see if a Replay ID is available
        var replayId = JavaSdk.ScopesAdapter.Instance?.Options?.ReplayController?.ReplayId?.ToSentryId();
        return (replayId is { } id && id != SentryId.Empty) ? id : null;
#else
        return null;
#endif
    }
}
