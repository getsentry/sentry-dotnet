using Sentry.Extensions.Logging;

namespace Sentry.Maui;

/// <inheritdoc cref="BindableSentryOptions"/>
internal class BindableSentryMauiOptions : BindableSentryLoggingOptions
{
    public bool? IncludeTextInBreadcrumbs { get; set; }
    public bool? IncludeTitleInBreadcrumbs { get; set; }
    public bool? IncludeBackgroundingStateInBreadcrumbs { get; set; }
    public bool? CreateElementEventsBreadcrumbs { get; set; } = false;
    public bool? AttachScreenshots { get; set; }

    public void ApplyTo(SentryMauiOptions options)
    {
        base.ApplyTo(options);
        options.IncludeTextInBreadcrumbs = IncludeTextInBreadcrumbs ?? options.IncludeTextInBreadcrumbs;
        options.IncludeTitleInBreadcrumbs = IncludeTitleInBreadcrumbs ?? options.IncludeTitleInBreadcrumbs;
        options.IncludeBackgroundingStateInBreadcrumbs = IncludeBackgroundingStateInBreadcrumbs?? options.IncludeBackgroundingStateInBreadcrumbs;
        options.CreateElementEventsBreadcrumbs = CreateElementEventsBreadcrumbs ?? options.CreateElementEventsBreadcrumbs;
        options.AttachScreenshots = AttachScreenshots ?? options.AttachScreenshots;
    }
}
