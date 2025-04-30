
using Sentry.Infrastructure;
#if __ANDROID__
using View = Android.Views.View;
using Android.Views;
using Java.Lang;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
#endif

namespace Sentry.Maui;

/// <summary>
/// Contains custom <see cref="BindableProperty"/> definitions used to control the behaviour of the Sentry SessionReplay
/// feature in MAUI apps.
/// <remarks>
/// NOTE: Session Replay is currently an experimental feature for MAUI and is subject to change.
/// </remarks>
/// </summary>
public static class SessionReplay
{
    /// <summary>
    /// Mask can be used to either unmask or mask a view.
    /// </summary>
    public static readonly BindableProperty MaskProperty =
        BindableProperty.CreateAttached(
            "Mask",
            typeof(SessionReplayMaskMode),
            typeof(SessionReplay),
            defaultValue: SessionReplayMaskMode.Mask,
            propertyChanged: OnMaskChanged);

    /// <summary>
    /// Gets the value of the Mask property for a view.
    /// </summary>
    public static SessionReplayMaskMode GetMask(BindableObject view) => (SessionReplayMaskMode)view.GetValue(MaskProperty);

    /// <summary>
    /// Sets the value of the Mask property for a view.
    /// </summary>
    /// <param name="view">The view element to mask or unmask</param>
    /// <param name="value">The value to assign. Can be either "sentry-mask" or "sentry-unmask".</param>
    public static void SetMask(BindableObject view, SessionReplayMaskMode value) => view.SetValue(MaskProperty, value);

    private static void OnMaskChanged(BindableObject bindable, object oldValue, object newValue)
    {
#if __ANDROID__
        if (bindable is not VisualElement ve || newValue is not SessionReplayMaskMode maskSetting)
        {
            return;
        }

        // This code looks pretty funky... just matching how funky MAUI is though.
        // See https://github.com/getsentry/sentry-dotnet/pull/4121#discussion_r2054129378
        ve.HandlerChanged -= OnMaskedElementHandlerChanged;
        ve.HandlerChanged -= OnUnmaskedElementHandlerChanged;

        if (maskSetting == SessionReplayMaskMode.Mask)
        {
            ve.HandlerChanged += OnMaskedElementHandlerChanged;
        }
        else if (maskSetting == SessionReplayMaskMode.Unmask)
        {
            ve.HandlerChanged += OnUnmaskedElementHandlerChanged;
        }
#endif
    }

#if __ANDROID__
    private static void OnMaskedElementHandlerChanged(object? sender, EventArgs _)
    {
        if ((sender as VisualElement)?.Handler?.PlatformView is not View nativeView)
        {
            return;
        }

        nativeView.Tag = SessionReplayMaskMode.Mask.ToNativeTag();
    }

    private static void OnUnmaskedElementHandlerChanged(object? sender, EventArgs _)
    {
        if ((sender as VisualElement)?.Handler?.PlatformView is not View nativeView)
        {
            return;
        }

        nativeView.Tag = SessionReplayMaskMode.Unmask.ToNativeTag();
    }
#endif
}
