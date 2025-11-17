#if __ANDROID__
using Sentry.Android.Extensions;
#endif

namespace Sentry.Internal;

internal interface IReplaySession
{
    public SentryId? ActiveReplayId { get; }
}

internal class ReplaySession : IReplaySession
{
    public static readonly IReplaySession Instance = new ReplaySession();

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
#elif __IOS__
            string? nativeId = null;
            SentryCocoaSdk.ConfigureScope(scope => nativeId = scope.ReplayId);
            return (nativeId is { } id) ? SentryId.Parse(id) : null;
#else
            return null;
#endif
        }
    }
}
