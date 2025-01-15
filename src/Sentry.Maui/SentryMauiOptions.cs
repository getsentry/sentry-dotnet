using Microsoft.Extensions.Logging;
using Sentry.Extensions.Logging;
using Sentry.Maui.Internal;

namespace Sentry.Maui;

/// <summary>
/// Sentry MAUI integration options
/// </summary>
public class SentryMauiOptions : SentryLoggingOptions
{
    /// <summary>
    /// Creates a new instance of <see cref="SentryMauiOptions"/>.
    /// </summary>
    public SentryMauiOptions()
    {
        // Set defaults for options that are different for MAUI.
        // The user can change these. If you want to force a value, use SentryMauiOptionsSetup instead.
        // Also, some of these are already set in the base Sentry SDK, but since we don't yet have native targets
        // there for all MAUI targets, we'll set them again here.

        AutoSessionTracking = true;
        DetectStartupTime = StartupTimeDetectionMode.Fast;
#if !PLATFORM_NEUTRAL
        CacheDirectoryPath = Microsoft.Maui.Storage.FileSystem.CacheDirectory;
#endif
    }

    /// <summary>
    /// Gets or sets whether elements that implement <see cref="IText"/>
    /// (such as <see cref="Button"/>, <see cref="Label"/>, <see cref="Entry"/>, and others)
    /// will have their text included on breadcrumbs.
    /// Use caution when enabling, as such values may contain personally identifiable information (PII).
    /// The default is <c>false</c> (exclude).
    /// </summary>
    public bool IncludeTextInBreadcrumbs { get; set; }

    /// <summary>
    /// Gets or sets whether elements that implement <see cref="ITitledElement"/>
    /// (such as <see cref="Window"/>, <see cref="Page"/>, and others)
    /// will have their titles included on breadcrumbs.
    /// Use caution when enabling, as such values may contain personally identifiable information (PII).
    /// The default is <c>false</c> (exclude).
    /// </summary>
    public bool IncludeTitleInBreadcrumbs { get; set; }

    /// <summary>
    /// Gets or sets whether the breadcrumb sent for the <see cref="Window.Backgrounding"/>
    /// event will include state data from <see cref="BackgroundingEventArgs.State"/>.
    /// Use caution when enabling, as such values may contain personally identifiable information (PII).
    /// The default is <c>false</c> (exclude).
    /// </summary>
    public bool IncludeBackgroundingStateInBreadcrumbs { get; set; }

    /// <summary>
    /// Gets or sets whether the SDK automatically binds to common <see cref=" Microsoft.Maui.Controls.Element"/> events
    /// like 'ChildAdded', 'ChildRemoved', 'ParentChanged' and 'BindingContextChanged'.
    /// Use caution when enabling, as depending on your application this might incur a performance overhead.
    /// </summary>
    public bool CreateElementEventsBreadcrumbs { get; set; } = false;

    /// <summary>
    /// Automatically attaches a screenshot of the app at the time of the event capture.
    /// </summary>
    /// <remarks>
    /// Make sure to only enable this feature if no sensitive data, such as PII, can be visible on the screen.
    /// Screenshots can be removed from some specific events during BeforeSend through the Hint.
    /// </remarks>
    public bool AttachScreenshot { get; set; }

    private Func<SentryEvent, SentryHint, bool>? _beforeCapture;
    /// <summary>
    /// Action performed before attaching a screenshot
    /// </summary>
    internal Func<SentryEvent, SentryHint, bool>? BeforeCaptureInternal => _beforeCapture;

    /// <summary>
    /// Configures a callback function to be invoked before taking a screenshot
    /// </summary>
    /// <remarks>
    /// if this callback return false the capture will not take place
    /// </remarks>
    /// <code>
    /// 
    ///options.SetBeforeCapture((@event, hint) =>
    ///{
    ///    // Return true to capture or false to prevent the capture
    ///    return true;
    ///});
    /// </code>
    /// <param name="beforeCapture">Callback to be executed before taking a screenshot</param>
    public void SetBeforeScreenshotCapture(Func<SentryEvent, SentryHint, bool> beforeCapture)
    {
        _beforeCapture = beforeCapture;
    }

#if ANDROID || IOS || MACCATALYST
    internal bool SessionReplayEnabled => Native.SessionReplay.SessionSampleRate > 0.0f || Native.SessionReplay.OnErrorSampleRate > 0.0f;
#endif
}
