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
        IsEnvironmentUser = false;
#if !PLATFORM_NEUTRAL
        CacheDirectoryPath = Microsoft.Maui.Storage.FileSystem.CacheDirectory;
#endif
    }

    internal List<IMauiElementEventBinderRegistration> IntegrationEventBinders { get; } = [];

    internal void AddIntegrationEventBinder<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TEventBinder>()
        where TEventBinder : class, IMauiElementEventBinder
    {
        IntegrationEventBinders.Add(new MauiElementEventBinderRegistration<TEventBinder>());
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

    /// <summary>
    /// Automatically starts Sentry transactions for navigation events (e.g. Shell navigation, modal push/pop)
    /// and, when <see cref="EnableUserInteractionTracing"/> is also enabled, for user-interaction events
    /// (e.g. Button clicks). The transaction is set on the scope so child spans (e.g. HTTP requests,
    /// database calls) can be attached.
    /// Transactions finish automatically after <see cref="AutoTransactionIdleTimeout"/> if not finished
    /// explicitly first (e.g. by a subsequent navigation).
    /// Requires <see cref="SentryOptions.TracesSampleRate"/> or <see cref="SentryOptions.TracesSampler"/> to
    /// be configured.
    /// The default is <c>true</c>.
    /// </summary>
    public bool EnableAutoTransactions { get; set; } = true;

    /// <summary>
    /// Controls how long an automatic transaction (navigation or user interaction) waits before finishing
    /// itself when not explicitly finished. Defaults to 3 seconds.
    /// </summary>
    public TimeSpan AutoTransactionIdleTimeout { get; set; } = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Automatically starts a Sentry transaction for user-interaction events (currently: Button clicks).
    /// Requires <see cref="EnableAutoTransactions"/> to be enabled as well as
    /// <see cref="SentryOptions.TracesSampleRate"/> or <see cref="SentryOptions.TracesSampler"/> to be
    /// configured. Interaction transactions are named <c>&lt;PageType&gt;.&lt;AutomationId|StyleId&gt;</c>.
    /// If the element has neither <see cref="Element.AutomationId"/> nor <see cref="Element.StyleId"/> the
    /// transaction is skipped and a warning is logged.
    /// The default is <c>true</c>.
    /// </summary>
    public bool EnableUserInteractionTracing { get; set; } = true;

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
}
