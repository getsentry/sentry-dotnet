namespace Sentry.Maui.Internal;

internal static class MauiEvents
{
    // These are only going to get set and used by `MauiEventsBinder`
    public static IHub Hub { private get; set; } = null!;
    public static SentryMauiOptions Options { private get; set; } = null!;

    // https://develop.sentry.dev/sdk/event-payloads/breadcrumbs/#breadcrumb-types
    // https://github.com/getsentry/sentry/blob/master/static/app/types/breadcrumbs.tsx
    internal const string NavigationType = "navigation";
    internal const string SystemType = "system";
    internal const string UserType = "user";
    internal const string LifecycleCategory =  "ui.lifecycle";
    internal const string NavigationCategory = "navigation";
    internal const string RenderingCategory = "ui.rendering";
    internal const string UserActionCategory = "ui.useraction";

    // Application Events

    public static void OnApplicationOnPageAppearing(object? sender, Page page) =>
        Hub.AddBreadcrumbForEvent(Options, sender, nameof(Application.PageAppearing), NavigationType, NavigationCategory, data => data.AddElementInfo(Options, page, nameof(Page)));
    public static void OnApplicationOnPageDisappearing(object? sender, Page page) =>
        Hub.AddBreadcrumbForEvent(Options, sender, nameof(Application.PageDisappearing), NavigationType, NavigationCategory, data => data.AddElementInfo(Options, page, nameof(Page)));
    public static void OnApplicationOnModalPushed(object? sender, ModalPushedEventArgs e) =>
        Hub.AddBreadcrumbForEvent(Options, sender, nameof(Application.ModalPushed), NavigationType, NavigationCategory, data => data.AddElementInfo(Options, e.Modal, nameof(e.Modal)));
    public static void OnApplicationOnModalPopped(object? sender, ModalPoppedEventArgs e) =>
        Hub.AddBreadcrumbForEvent(Options, sender, nameof(Application.ModalPopped), NavigationType, NavigationCategory, data => data.AddElementInfo(Options, e.Modal, nameof(e.Modal)));
    public static void OnApplicationOnRequestedThemeChanged(object? sender, AppThemeChangedEventArgs e) =>
        Hub.AddBreadcrumbForEvent(Options, sender, nameof(Application.RequestedThemeChanged), SystemType, RenderingCategory, data => data.Add(nameof(e.RequestedTheme), e.RequestedTheme.ToString()));

    // Window Events

    public static void OnWindowOnActivated(object? sender, EventArgs _) =>
        Hub.AddBreadcrumbForEvent(Options, sender, nameof(Window.Activated), SystemType, LifecycleCategory);

    public static void OnWindowOnDeactivated(object? sender, EventArgs _) =>
        Hub.AddBreadcrumbForEvent(Options, sender, nameof(Window.Deactivated), SystemType, LifecycleCategory);

    public static void OnWindowOnStopped(object? sender, EventArgs _) =>
        Hub.AddBreadcrumbForEvent(Options, sender, nameof(Window.Stopped), SystemType, LifecycleCategory);

    public static void OnWindowOnResumed(object? sender, EventArgs _) =>
        Hub.AddBreadcrumbForEvent(Options, sender, nameof(Window.Resumed), SystemType, LifecycleCategory);

    public static void OnWindowOnCreated(object? sender, EventArgs _) =>
        Hub.AddBreadcrumbForEvent(Options, sender, nameof(Window.Created), SystemType, LifecycleCategory);

    public static void OnWindowOnDestroying(object? sender, EventArgs _) =>
        Hub.AddBreadcrumbForEvent(Options, sender, nameof(Window.Destroying), SystemType, LifecycleCategory);

    public static void OnWindowOnBackgrounding(object? sender, BackgroundingEventArgs e) =>
        Hub.AddBreadcrumbForEvent(Options, sender, nameof(Window.Backgrounding), SystemType, LifecycleCategory, data =>
        {
            if (!Options?.IncludeBackgroundingStateInBreadcrumbs ?? true)
            {
                return;
            }

            foreach (var item in e.State)
            {
                data.Add($"{nameof(e.State)}.{item.Key}", item.Value ?? "<null>");
            }
        });

    public static void OnWindowOnDisplayDensityChanged(object? sender, DisplayDensityChangedEventArgs e) =>
        Hub.AddBreadcrumbForEvent(Options, sender, nameof(Window.DisplayDensityChanged), SystemType, LifecycleCategory, data =>
        {
            var displayDensity = e.DisplayDensity.ToString(CultureInfo.InvariantCulture);
            data.Add(nameof(e.DisplayDensity), displayDensity);
        });

    public static void OnWindowOnModalPushed(object? sender, ModalPushedEventArgs e) =>
        Hub.AddBreadcrumbForEvent(Options, sender, nameof(Window.ModalPushed), NavigationType, NavigationCategory, data => data.AddElementInfo(Options, e.Modal, nameof(e.Modal)));

    public static void OnWindowOnModalPopped(object? sender, ModalPoppedEventArgs e) =>
        Hub.AddBreadcrumbForEvent(Options, sender, nameof(Window.ModalPopped), NavigationType, NavigationCategory, data => data.AddElementInfo(Options, e.Modal, nameof(e.Modal)));

    public static void OnWindowOnPopCanceled(object? sender, EventArgs _) =>
        Hub.AddBreadcrumbForEvent(Options, sender, nameof(Window.PopCanceled), NavigationType, NavigationCategory);

    // Element Events

    public static void OnElementOnChildAdded(object? sender, ElementEventArgs e) =>
        Hub.AddBreadcrumbForEvent(Options, sender, nameof(Element.ChildAdded), SystemType, RenderingCategory, data => data.AddElementInfo(Options, e.Element, nameof(e.Element)));

    public static void OnElementOnChildRemoved(object? sender, ElementEventArgs e) =>
        Hub.AddBreadcrumbForEvent(Options, sender, nameof(Element.ChildRemoved), SystemType, RenderingCategory, data => data.AddElementInfo(Options, e.Element, nameof(e.Element)));

    public static void OnElementOnParentChanged(object? sender, EventArgs _) =>
        Hub.AddBreadcrumbForEvent(Options, sender, nameof(Element.ParentChanged), SystemType, RenderingCategory, data =>
        {
            var e = sender as Element;
            data.AddElementInfo(Options, e?.Parent, nameof(e.Parent));
        });

    public static void OnElementOnBindingContextChanged(object? sender, EventArgs _)
    {
        if (sender is not BindableObject { BindingContext: { } bindingContext })
        {
            // Don't add breadcrumbs when BindingContext is null
            return;
        }

        var e = sender as Element;
        Hub.AddBreadcrumbForEvent(Options, e, nameof(BindableObject.BindingContextChanged), SystemType, RenderingCategory, data => data.Add(nameof(BindableObject.BindingContext), bindingContext.ToStringOrTypeName()));
    }

    // Visual Events

    public static void OnElementOnFocused(object? sender, FocusEventArgs _) =>
        Hub.AddBreadcrumbForEvent(Options, sender, nameof(VisualElement.Focused), SystemType, RenderingCategory);

    public static void OnElementOnUnfocused(object? sender, FocusEventArgs _) =>
        Hub.AddBreadcrumbForEvent(Options, sender, nameof(VisualElement.Unfocused), SystemType, RenderingCategory);

    // Shell Events

    public static void OnShellOnNavigating(object? sender, ShellNavigatingEventArgs e) =>
        Hub.AddBreadcrumbForEvent(Options, sender, nameof(Shell.Navigating), NavigationType, NavigationCategory, data =>
        {
            data.Add("from", e.Current?.Location.ToString() ?? "");
            data.Add("to", e.Target?.Location.ToString() ?? "");
            data.Add(nameof(e.Source), e.Source.ToString());
        });

    public static void OnShellOnNavigated(object? sender, ShellNavigatedEventArgs e) =>
        Hub.AddBreadcrumbForEvent(Options, sender, nameof(Shell.Navigated), NavigationType, NavigationCategory, data =>
        {
            data.Add("from", e.Previous?.Location.ToString() ?? "");
            data.Add("to", e.Current?.Location.ToString() ?? "");
            data.Add(nameof(e.Source), e.Source.ToString());
        });

    // Page Events

    public static void OnPageOnAppearing(object? sender, EventArgs _) =>
        Hub.AddBreadcrumbForEvent(Options, sender, nameof(Page.Appearing), SystemType, LifecycleCategory);

    public static void OnPageOnDisappearing(object? sender, EventArgs _) =>
        Hub.AddBreadcrumbForEvent(Options, sender, nameof(Page.Disappearing), SystemType, LifecycleCategory);

    public static void OnPageOnNavigatedTo(object? sender, NavigatedToEventArgs e) =>
        Hub.AddBreadcrumbForEvent(Options, sender, nameof(Page.NavigatedTo), NavigationType, NavigationCategory, data => data.AddElementInfo(Options, e.GetPreviousPage(), "PreviousPage"));

    public static void OnPageOnLayoutChanged(object? sender, EventArgs _) =>
        Hub.AddBreadcrumbForEvent(Options, sender, nameof(Page.LayoutChanged), SystemType, RenderingCategory);

    // Button Events

    public static void OnButtonOnClicked(object? sender, EventArgs _) =>
        Hub.AddBreadcrumbForEvent(Options, sender, nameof(Button.Clicked), UserType, UserActionCategory);

    public static void OnButtonOnPressed(object? sender, EventArgs _) =>
        Hub.AddBreadcrumbForEvent(Options, sender, nameof(Button.Pressed), UserType, UserActionCategory);

    public static void OnButtonOnReleased(object? sender, EventArgs _) =>
        Hub.AddBreadcrumbForEvent(Options, sender, nameof(Button.Released), UserType, UserActionCategory);
}
