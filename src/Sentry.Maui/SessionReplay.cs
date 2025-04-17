#if __ANDROID__
using View = Android.Views.View;
#endif

namespace Sentry.Maui;

/// <summary>
/// Contains custom <see cref="BindableProperty"/> definitions used to control the behaviour of the Sentry SessionReplay
/// feature in MAUI apps.
/// </summary>
public static class SessionReplay
{
    /// <summary>
    /// Mask can be used to either unmask or mask a view.
    /// </summary>
    public static readonly BindableProperty MaskProperty =
        BindableProperty.CreateAttached(
            "Mask",
            typeof(string),
            typeof(SessionReplay),
            defaultValue: "sentry-mask", // default: masked
            propertyChanged: OnMaskChanged);

    /// <summary>
    /// Gets the value of the Mask property for a view
    /// </summary>
    public static string GetMask(BindableObject view) => (string)view.GetValue(MaskProperty);

    /// <summary>
    /// Sets the value of the Mask property for a view. .
    /// </summary>
    /// <param name="view">The view element to mask or unmask</param>
    /// <param name="value">The value to assign. Can be either "sentry-mask" or "sentry-unmask".</param>
    public static void SetMask(BindableObject view, string value) => view.SetValue(MaskProperty, value);

    private static void OnMaskChanged(BindableObject bindable, object oldValue, object newValue)
    {
#if __ANDROID__
            if (bindable is VisualElement ve)
            {
                ve.HandlerChanged += (s, e) =>
                {
                    if (ve.Handler?.PlatformView is View nativeView && newValue is string maskSetting)
                    {
                        if (string.IsNullOrEmpty(maskSetting))
                        {
                            return;
                        }
                        // "sentry-unmask" if true; remove tag if false
                        nativeView.Tag = maskSetting;
                    }
                };
            }
#endif
    }
}
