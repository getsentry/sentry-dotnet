
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
            if (bindable is VisualElement ve)
            {
                ve.HandlerChanged += (s, e) =>
                {
                    if (ve.Handler?.PlatformView is not View nativeView ||
                        newValue is not SessionReplayMaskMode maskSetting)
                    {
                        return;
                    }

                    nativeView.Tag = maskSetting.ToNativeTag();
                };
            }
#endif
    }
}
