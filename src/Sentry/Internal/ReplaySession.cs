#if __ANDROID__
using Sentry.Android.Extensions;
#endif

namespace Sentry.Internal;

// TODO: This static class is pretty ugly... let's refactor it into an IReplaySession interface so that we can
// inject a mock in unit tests. If no IReplaySession is provided to the various classes that need it then we can
// fall back to a singleton instance of this class.
//
// We should be able to remove the ReplayFixture then as well (which is ugly - it forces us to initialise the test
// replay id for all tests in a Test class... which makes it difficult to test alternate scenarios).
internal interface IReplaySession
{
    public SentryId? ActiveReplayId { get; }
}

internal class ReplaySession : IReplaySession
{
    public static readonly IReplaySession Instance = new ReplaySession();

    internal static readonly IReplaySession DisabledInstance = new DisabledReplaySession();

    private ReplaySession()
    {
    }

    public SentryId? ActiveReplayId
    {
        get
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

    private class DisabledReplaySession : IReplaySession
    {
        public SentryId? ActiveReplayId => null;
    }
}
