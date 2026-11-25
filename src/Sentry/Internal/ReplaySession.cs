using System.Runtime.ExceptionServices;
using Sentry.Extensibility;
#if __ANDROID__
using Java.Interop;
#elif __IOS__
using CoreFoundation;
#endif

namespace Sentry.Internal;

internal interface IReplaySession
{
    public SentryId? ActiveReplayId { get; }

    /// <summary>
    /// Captures the replay for an error event that is about to be sent.
    /// </summary>
    /// <returns>The id of the replay the event belongs to, or null when the event has no replay.</returns>
    public SentryId? CaptureReplay(SentryEvent @event);
}

internal class ReplaySession : IReplaySession
{
    public static readonly IReplaySession Instance = new ReplaySession();

    internal static IReplaySession? PlatformDefault =>
#if __ANDROID__ || __IOS__
        Instance;
#else
        null;
#endif

    internal static readonly TimeSpan MainThreadCaptureTimeout = TimeSpan.FromSeconds(1);

    private ReplaySession()
    {
    }

    public SentryId? ActiveReplayId
    {
        get
        {
#if __ANDROID__ || __IOS__
            if (SentrySdk.CurrentOptions?.Native.ExperimentalOptions.SessionReplay.IsSessionReplayEnabled != true)
            {
                return null;
            }
#endif
#if __ANDROID__
            try
            {
                return JavaScopes.ReadReplayId();
            }
            catch (Exception e)
            {
                SentrySdk.CurrentOptions?.LogError(e, "Failed to read the replay id from the Java scope.");
                return null;
            }
#elif __IOS__
            string? nativeId = null;
            Sentry.Cocoa.CocoaScope.Configure(scope => nativeId = scope.ReplayId);
            return ToReplayId(nativeId);
#else
            return null;
#endif
        }
    }

#if __ANDROID__
    public SentryId? CaptureReplay(SentryEvent @event)
    {
        if (SentrySdk.CurrentOptions?.Native.ExperimentalOptions.SessionReplay.IsSessionReplayEnabled != true)
        {
            return null;
        }

        var capturedId = JavaSdk.ScopesAdapter.Instance?.Options?.ReplayController?.CaptureReplay(JavaBoolean.ValueOf(IsCrash(@event)));
        return ToReplayId(capturedId?.ToString()) ?? ActiveReplayId;
    }
#elif __IOS__
    public SentryId? CaptureReplay(SentryEvent @event) =>
        CaptureCocoaReplay(SentrySdk.CurrentOptions, NSThread.IsMain, work => DispatchQueue.MainQueue.DispatchAsync(work));
#else
    public SentryId? CaptureReplay(SentryEvent @event) => null;
#endif

    internal static SentryId? ToReplayId(string? nativeId) =>
        Guid.TryParse(nativeId, out var guid) && guid != Guid.Empty ? new SentryId(guid) : null;

    // Narrower than HasUnhandledTerminalException(): Java defers a terminating capture's replay to the next launch and stops the replay from ever switching to session mode.
    internal static bool IsCrash(SentryEvent @event) =>
        @event.SentryExceptions?.Any(exception => exception.Mechanism is { } mechanism && (
            mechanism.Terminal == true ||
            mechanism is { Handled: false, Type: "AppDomain.UnhandledException" or "UnhandledExceptionRaiser" })) == true;

    internal static SentryId? CaptureOnMainThread(
        Func<SentryId?> capture,
        Func<SentryId?> timedOutFallback,
        bool isMainThread,
        Action<Action> dispatchToMainThread,
        TimeSpan timeout,
        SentryOptions? options)
    {
        if (isMainThread)
        {
            return capture();
        }

        SentryId? replayId = null;
        Exception? error = null;
        // Not disposed: after a timeout the dispatched capture still runs and sets it.
        var captured = new ManualResetEventSlim();
        dispatchToMainThread(() =>
        {
            try
            {
                replayId = capture();
            }
            catch (Exception e)
            {
                error = e;
            }
            finally
            {
                captured.Set();
            }
        });

        if (!captured.Wait(timeout))
        {
            options?.LogWarning("Capturing the replay on the main thread timed out after {0}. The event is sent with the active replay id, if any.", timeout);
            return timedOutFallback();
        }

        if (error is not null)
        {
            ExceptionDispatchInfo.Capture(error).Throw();
        }

        return replayId;
    }

#if __IOS__
    internal SentryId? CaptureCocoaReplay(SentryOptions? options, bool isMainThread, Action<Action> dispatchToMainThread)
    {
        if (options?.Native.ExperimentalOptions.SessionReplay.IsSessionReplayEnabled != true)
        {
            return null;
        }

        return CaptureOnMainThread(
            CaptureNativeReplay,
            () => ActiveReplayId,
            isMainThread,
            dispatchToMainThread,
            MainThreadCaptureTimeout,
            options);
    }

    private static SentryId? CaptureNativeReplay()
    {
        var replay = SentryCocoaHybridSdk.Internal.Replay;
        return replay.Capture() ? ToReplayId(replay.ReplayId) : null;
    }
#endif

#if __ANDROID__
    // The binding doesn't generate io.sentry.IScope, so the scopes are read through JNI,
    // in the order Java's CombinedScopeView reads them.
    internal static class JavaScopes
    {
        private const string ScopeSignature = "()Lio/sentry/IScope;";

        private static readonly JniType ScopesType = new("io/sentry/IScopes");
        private static readonly JniType ScopeType = new("io/sentry/IScope");
        private static readonly JniType ObjectType = new("java/lang/Object");

        private static readonly JniMethodInfo[] ScopeGetters =
        [
            ScopesType.GetInstanceMethod("getScope", ScopeSignature),
            ScopesType.GetInstanceMethod("getIsolationScope", ScopeSignature),
            ScopesType.GetInstanceMethod("getGlobalScope", ScopeSignature),
        ];

        private static readonly JniMethodInfo GetReplayId =
            ScopeType.GetInstanceMethod("getReplayId", "()Lio/sentry/protocol/SentryId;");

        private static readonly JniMethodInfo ObjectToString =
            ObjectType.GetInstanceMethod("toString", "()Ljava/lang/String;");

        internal static SentryId? ReadReplayId()
        {
            var currentScopes = JavaSdk.Sentry.CurrentScopes;
            var scopes = new JniObjectReference(currentScopes.Handle);
            try
            {
                foreach (var getScope in ScopeGetters)
                {
                    var scope = JniEnvironment.InstanceMethods.CallObjectMethod(scopes, getScope);
                    try
                    {
                        if (ReadReplayId(scope) is { } replayId)
                        {
                            return replayId;
                        }
                    }
                    finally
                    {
                        JniObjectReference.Dispose(ref scope);
                    }
                }

                return null;
            }
            finally
            {
                GC.KeepAlive(currentScopes);
            }
        }

        private static SentryId? ReadReplayId(JniObjectReference scope)
        {
            if (!scope.IsValid)
            {
                return null;
            }

            var replayId = JniEnvironment.InstanceMethods.CallObjectMethod(scope, GetReplayId);
            try
            {
                if (!replayId.IsValid)
                {
                    return null;
                }

                var text = JniEnvironment.InstanceMethods.CallObjectMethod(replayId, ObjectToString);
                return ToReplayId(JniEnvironment.Strings.ToString(ref text, JniObjectReferenceOptions.CopyAndDispose));
            }
            finally
            {
                JniObjectReference.Dispose(ref replayId);
            }
        }
    }
#endif
}
