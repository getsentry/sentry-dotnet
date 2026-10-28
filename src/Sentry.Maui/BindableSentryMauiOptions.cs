using Sentry.Extensions.Logging;

namespace Sentry.Maui;

/// <inheritdoc cref="BindableSentryOptions"/>
internal class BindableSentryMauiOptions : BindableSentryLoggingOptions
{
    public bool? IncludeTextInBreadcrumbs { get; set; }
    public bool? IncludeButtonTextInBreadcrumbs { get; set; }
    public bool? IncludeTitleInBreadcrumbs { get; set; }
    public bool? IncludeBackgroundingStateInBreadcrumbs { get; set; }
    public bool? CreateElementEventsBreadcrumbs { get; set; }
    public bool? AttachScreenshot { get; set; }

    public void ApplyTo(SentryMauiOptions options)
    {
        base.ApplyTo(options);
        options.IncludeTextInBreadcrumbs = IncludeTextInBreadcrumbs ?? options.IncludeTextInBreadcrumbs;
        options.IncludeButtonTextInBreadcrumbs = IncludeButtonTextInBreadcrumbs ?? options.IncludeButtonTextInBreadcrumbs;
        options.IncludeTitleInBreadcrumbs = IncludeTitleInBreadcrumbs ?? options.IncludeTitleInBreadcrumbs;
        options.IncludeBackgroundingStateInBreadcrumbs = IncludeBackgroundingStateInBreadcrumbs ?? options.IncludeBackgroundingStateInBreadcrumbs;
        options.CreateElementEventsBreadcrumbs = CreateElementEventsBreadcrumbs ?? options.CreateElementEventsBreadcrumbs;
        options.AttachScreenshot = AttachScreenshot ?? options.AttachScreenshot;
    }
}
