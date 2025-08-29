using Microsoft.Extensions.Options;
using Sentry.Extensibility;
#if __ANDROID__
using View = Android.Views.View;
#endif

namespace Sentry.Maui.Internal;

/// <summary>
/// Masks or unmasks visual elements for session replay recordings
/// </summary>
internal class MauiSessionReplayMaskControlsOfTypeBinder : IMauiElementEventBinder
{
    private readonly SentryMauiOptions _options;

    internal bool IsEnabled { get; }

    public MauiSessionReplayMaskControlsOfTypeBinder(SentryMauiOptions options)
    {
        _options = options;
#if __ANDROID__
        var replayOptions = options.Native.ExperimentalOptions.SessionReplay;
        IsEnabled = replayOptions is { IsSessionReplayEnabled: true, IsTypeMaskingUsed: true };
#else
        IsEnabled = false;
#endif
    }

    /// <inheritdoc />
    public void Bind(VisualElement element, Action<BreadcrumbEvent> _)
    {
        element.Loaded += OnElementLoaded;
    }

    /// <inheritdoc />
    public void UnBind(VisualElement element)
    {
        element.Loaded -= OnElementLoaded;
    }

    internal void OnElementLoaded(object? sender, EventArgs _)
    {
        if (sender is not VisualElement element)
        {
            _options.LogDebug("OnElementLoaded: sender is not a VisualElement");
            return;
        }

        var handler = element.Handler;
        if (handler is null)
        {
            _options.LogDebug("OnElementLoaded: element.Handler is null");
            return;
        }

#if __ANDROID__
        if (element.Handler?.PlatformView is not View nativeView)
        {
            return;
        }

        if (_options.Native.ExperimentalOptions.SessionReplay.MaskedControls.FirstOrDefault(maskType => element.GetType().IsAssignableFrom(maskType)) is not null)
        {
            nativeView.Tag = SessionReplayMaskMode.Mask.ToNativeTag();
            _options.LogDebug("OnElementLoaded: Successfully set sentry-mask tag on native view");
        }
        else if (_options.Native.ExperimentalOptions.SessionReplay.UnmaskedControls.FirstOrDefault(unmaskType => element.GetType().IsAssignableFrom(unmaskType)) is not null)
        {
            nativeView.Tag = SessionReplayMaskMode.Unmask.ToNativeTag();
            _options.LogDebug("OnElementLoaded: Successfully set sentry-unmask tag on native view");
        }
#endif
    }
}
