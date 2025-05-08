namespace Sentry.Maui;

/// <summary>
/// Controls the masking behaviour of the Session Replay feature.
/// </summary>
public enum SessionReplayMaskMode
{
    /// <summary>
    /// Masks the view
    /// </summary>
    Mask,
    /// <summary>
    /// Unmasks the view
    /// </summary>
    Unmask
}

internal static class SessionReplayMaskModeExtensions
{
#if __ANDROID__
    /// <summary>
    /// Maps from <see cref="SessionReplayMaskMode"/> to the native tag values used by the JavaSDK to mask and unmask
    /// views. See https://docs.sentry.io/platforms/android/session-replay/privacy/#mask-by-view-instance
    /// </summary>
    public static string ToNativeTag(this SessionReplayMaskMode maskMode) => maskMode switch
    {
        SessionReplayMaskMode.Mask => "sentry-mask",
        SessionReplayMaskMode.Unmask => "sentry-unmask",
        _ => throw new ArgumentOutOfRangeException(nameof(maskMode), maskMode, null)
    };
#endif
}
