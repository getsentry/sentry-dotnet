#if __ANDROID__
using Sentry.Android.Extensions;
#endif

namespace Sentry.Internal;

internal static class ReplayHelper
{
    internal static SentryId? GetReplayId()
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
