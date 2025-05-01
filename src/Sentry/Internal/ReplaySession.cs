#if __ANDROID__
using Sentry.Android.Extensions;
#endif

namespace Sentry.Internal;

internal static class ReplaySession
{
    internal static Lazy<SentryId?> TestReplayId { get; } = new(() => SentryId.Create());

    private static Func<SentryId?>? TestReplayIdResolver;

    /// <summary>
    /// Initialises the test replay id resolver so that unit tests return a test id (rather than trying to resovle an
    /// ID from static platform libraries).
    /// </summary>
    internal static void InitTestReplayId()
    {
        TestReplayIdResolver = () => TestReplayId.Value;
    }

    internal static SentryId? GetReplayId() => (TestReplayIdResolver ?? ReplayIdResolver)();

    private static SentryId? ReplayIdResolver()
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
