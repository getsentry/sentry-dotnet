using System;
using Microsoft.Extensions.Options;
using Microsoft.Maui.Controls;
using Sentry.Internal;
using Sentry.Protocol;

namespace Sentry.Maui.Internal;

internal interface IMauiEventsBinder
{
    void HandleApplicationEvents(Application application, bool bind = true);
}

internal class MauiEventsBinder : IMauiEventsBinder
{
    private readonly IHub _hub;
    private readonly SentryMauiOptions _options;
    private readonly IEnumerable<IMauiElementEventBinder> _elementEventBinders;
    private readonly IEnumerable<IMauiPageEventHandler> _pageEventHandlers;

    // https://develop.sentry.dev/sdk/event-payloads/breadcrumbs/#breadcrumb-types
    // https://github.com/getsentry/sentry/blob/master/static/app/types/breadcrumbs.tsx
    internal const string NavigationType = "navigation";
    internal const string SystemType = "system";
    internal const string UserType = "user";
    internal const string LifecycleCategory = "ui.lifecycle";
    internal const string NavigationCategory = "navigation";
    internal const string RenderingCategory = "ui.rendering";
    internal const string UserActionCategory = "ui.useraction";


    public MauiEventsBinder(IHub hub, IOptions<SentryMauiOptions> options, IEnumerable<IMauiElementEventBinder> elementEventBinders, IEnumerable<IMauiPageEventHandler> pageEventHandlers)
    {
        _hub = hub;
        _options = options.Value;
        _elementEventBinders = elementEventBinders;
        _pageEventHandlers = pageEventHandlers;
    }

    public void HandleApplicationEvents(Application application, bool bind = true)
    {
        if (bind)
        {
            // Attach element events to all descendents as they are added to the application.
            application.DescendantAdded += OnApplicationOnDescendantAdded;
            application.DescendantRemoved += OnApplicationOnDescendantRemoved;

            if (_options.CreateElementEventsBreadcrumbs)
            {
                // The application is an element itself, so attach element events here also.
                HandleElementEvents(application);
            }

            // Navigation events
            application.PageAppearing += OnApplicationOnPageAppearing;
            application.PageDisappearing += OnApplicationOnPageDisappearing;
            application.ModalPushed += OnApplicationOnModalPushed;
            application.ModalPopped += OnApplicationOnModalPopped;

            // Theme changed event
            // https://docs.microsoft.com/dotnet/maui/user-interface/system-theme-changes#react-to-theme-changes
            application.RequestedThemeChanged += OnApplicationOnRequestedThemeChanged;
        }
        else
        {
            application.DescendantAdded -= OnApplicationOnDescendantAdded;
            application.DescendantRemoved -= OnApplicationOnDescendantRemoved;

            HandleElementEvents(application, bind: false);

            // Navigation events
            application.PageAppearing -= OnApplicationOnPageAppearing;
            application.PageDisappearing -= OnApplicationOnPageDisappearing;
            application.ModalPushed -= OnApplicationOnModalPushed;
            application.ModalPopped -= OnApplicationOnModalPopped;

            // Theme changed event
            application.RequestedThemeChanged -= OnApplicationOnRequestedThemeChanged;
        }
    }

    internal void OnApplicationOnDescendantAdded(object? _, ElementEventArgs e)
    {
        if (_options.CreateElementEventsBreadcrumbs)
        {
            // All elements have a set of common events we can hook
            HandleElementEvents(e.Element);
        }

        if (e.Element is VisualElement visualElement)
        {
            HandleVisualElementEvents(visualElement);
        }

        // We can also attach to specific events on built-in controls
        // Be sure to update ExplicitlyHandledTypes when adding to this list
        switch (e.Element)
        {
            case Window window:
                HandleWindowEvents(window);
                break;
            case Shell shell:
                HandleShellEvents(shell);
                break;
            case Page page:
                HandlePageEvents(page);
                break;
            default:
                if (e.Element is VisualElement ve)
                {
                    foreach (var binder in _elementEventBinders)
                    {
                        binder.Bind(ve, OnBreadcrumbCreateCallback);
                    }
                }
                break;
        }
    }

    internal void OnBreadcrumbCreateCallback(BreadcrumbEvent breadcrumb)
    {
        _hub.AddBreadcrumbForEvent(
            _options,
            breadcrumb.Sender,
            breadcrumb.EventName,
            UserType,
            UserActionCategory
        );
    }

    internal void OnApplicationOnDescendantRemoved(object? _, ElementEventArgs e)
    {
        // All elements have a set of common events we can hook
        HandleElementEvents(e.Element, bind: false);

        if (e.Element is VisualElement visualElement)
        {
            HandleVisualElementEvents(visualElement, bind: false);
        }

        // We can also attach to specific events on built-in controls
        // Be sure to update ExplicitlyHandledTypes when adding to this list
        switch (e.Element)
        {
            case Window window:
                HandleWindowEvents(window, bind: false);
                break;
            case Shell shell:
                HandleShellEvents(shell, bind: false);
                break;
            case Page page:
                HandlePageEvents(page, bind: false);
                break;
            default:
                if (e.Element is VisualElement ve)
                {
                    foreach (var binder in _elementEventBinders)
                    {
                        binder.UnBind(ve);
                    }
                }
                break;
        }
    }

    internal void HandleWindowEvents(Window window, bool bind = true)
    {
        if (bind)
        {
            // Lifecycle Events
            // https://docs.microsoft.com/dotnet/maui/fundamentals/windows
            // https://docs.microsoft.com/dotnet/maui/fundamentals/app-lifecycle#cross-platform-lifecycle-events

            // Lifecycle events caused by user action
            window.Activated += OnWindowOnActivated;
            window.Deactivated += OnWindowOnDeactivated;
            window.Stopped += OnWindowOnStopped;
            window.Resumed += OnWindowOnResumed;

            // System generated lifecycle events
            window.Created += OnWindowOnCreated;
            window.Destroying += OnWindowOnDestroying;
            window.Backgrounding += OnWindowOnBackgrounding;
            window.DisplayDensityChanged += OnWindowOnDisplayDensityChanged;

            // Navigation events
            window.ModalPushed += OnWindowOnModalPushed;
            window.ModalPopped += OnWindowOnModalPopped;
            window.PopCanceled += OnWindowOnPopCanceled;
        }
        else
        {
            // Lifecycle events caused by user action
            window.Activated -= OnWindowOnActivated;
            window.Deactivated -= OnWindowOnDeactivated;
            window.Stopped -= OnWindowOnStopped;
            window.Resumed -= OnWindowOnResumed;

            // System generated lifecycle events
            window.Created -= OnWindowOnCreated;
            window.Destroying -= OnWindowOnDestroying;
            window.Backgrounding -= OnWindowOnBackgrounding;
            window.DisplayDensityChanged -= OnWindowOnDisplayDensityChanged;

            // Navigation events
            window.ModalPushed -= OnWindowOnModalPushed;
            window.ModalPopped -= OnWindowOnModalPopped;
            window.PopCanceled -= OnWindowOnPopCanceled;
        }
    }

    internal void HandleElementEvents(Element element, bool bind = true)
    {
        if (bind)
        {
            // Rendering events
            element.ChildAdded += OnElementOnChildAdded;
            element.ChildRemoved += OnElementOnChildRemoved;
            element.ParentChanged += OnElementOnParentChanged;

            // These lead to lots of duplicate information, so probably best not to include them.
            // element.DescendantAdded
            // element.DescendantRemoved

            // BindableObject events
            element.BindingContextChanged += OnElementOnBindingContextChanged;
        }
        else
        {
            // Rendering events
            element.ChildAdded -= OnElementOnChildAdded;
            element.ChildRemoved -= OnElementOnChildRemoved;
            element.ParentChanged -= OnElementOnParentChanged;

            // BindableObject events
            element.BindingContextChanged -= OnElementOnBindingContextChanged;
        }
    }

    internal void HandleVisualElementEvents(VisualElement element, bool bind = true)
    {
        if (bind)
        {
            element.Focused += OnElementOnFocused;
            element.Unfocused += OnElementOnUnfocused;
        }
        else
        {
            element.Focused -= OnElementOnFocused;
            element.Unfocused -= OnElementOnUnfocused;
        }
    }

    internal void HandleShellEvents(Shell shell, bool bind = true)
    {
        if (bind)
        {
            // Navigation events
            // https://docs.microsoft.com/dotnet/maui/fundamentals/shell/navigation
            shell.Navigating += OnShellOnNavigating;
            shell.Navigated += OnShellOnNavigated;

            // A Shell is also a Page
            HandlePageEvents(shell);
        }
        else
        {
            // Navigation events
            // https://docs.microsoft.com/dotnet/maui/fundamentals/shell/navigation
            shell.Navigating -= OnShellOnNavigating;
            shell.Navigated -= OnShellOnNavigated;

            // A Shell is also a Page
            HandlePageEvents(shell, bind: false);
        }
    }

    internal void HandlePageEvents(Page page, bool bind = true)
    {
        if (bind)
        {
            // Lifecycle events
            // https://docs.microsoft.com/dotnet/maui/fundamentals/shell/lifecycle
            page.Appearing += OnPageOnAppearing;
            page.Disappearing += OnPageOnDisappearing;

            // Navigation events
            // https://github.com/dotnet/docs-maui/issues/583
            page.NavigatedTo += OnPageOnNavigatedTo;

            // Layout changed event
            // https://docs.microsoft.com/dotnet/api/xamarin.forms.ilayout.layoutchanged
            page.LayoutChanged += OnPageOnLayoutChanged;
        }
        else
        {
            // Lifecycle events
            // https://docs.microsoft.com/dotnet/maui/fundamentals/shell/lifecycle
            page.Appearing -= OnPageOnAppearing;
            page.Disappearing -= OnPageOnDisappearing;

            // Navigation events
            // https://github.com/dotnet/docs-maui/issues/583
            page.NavigatedTo -= OnPageOnNavigatedTo;

            // Layout changed event
            // https://docs.microsoft.com/dotnet/api/xamarin.forms.ilayout.layoutchanged
            page.LayoutChanged -= OnPageOnLayoutChanged;
        }
    }

    // Application Events

    private void OnApplicationOnPageAppearing(object? sender, Page page)
    {
        _hub.AddBreadcrumbForEvent(_options, sender, nameof(Application.PageAppearing), NavigationType, NavigationCategory, data => data.AddElementInfo(_options, page, nameof(Page)));
        RunPageEventHandlers(handler => handler.OnAppearing(page));
    }

    private void OnApplicationOnPageDisappearing(object? sender, Page page)
    {
        _hub.AddBreadcrumbForEvent(_options, sender, nameof(Application.PageDisappearing), NavigationType, NavigationCategory, data => data.AddElementInfo(_options, page, nameof(Page)));
        RunPageEventHandlers(handler => handler.OnDisappearing(page));
    }

    private void OnApplicationOnModalPushed(object? sender, ModalPushedEventArgs e) =>
        _hub.AddBreadcrumbForEvent(_options, sender, nameof(Application.ModalPushed), NavigationType, NavigationCategory, data => data.AddElementInfo(_options, e.Modal, nameof(e.Modal)));
    private void OnApplicationOnModalPopped(object? sender, ModalPoppedEventArgs e) =>
        _hub.AddBreadcrumbForEvent(_options, sender, nameof(Application.ModalPopped), NavigationType, NavigationCategory, data => data.AddElementInfo(_options, e.Modal, nameof(e.Modal)));
    private void OnApplicationOnRequestedThemeChanged(object? sender, AppThemeChangedEventArgs e) =>
        _hub.AddBreadcrumbForEvent(_options, sender, nameof(Application.RequestedThemeChanged), SystemType, RenderingCategory, data => data.Add(nameof(e.RequestedTheme), e.RequestedTheme.ToString()));

    // Window Events

    private void OnWindowOnActivated(object? sender, EventArgs _) =>
        _hub.AddBreadcrumbForEvent(_options, sender, nameof(Window.Activated), SystemType, LifecycleCategory);

    private void OnWindowOnDeactivated(object? sender, EventArgs _) =>
        _hub.AddBreadcrumbForEvent(_options, sender, nameof(Window.Deactivated), SystemType, LifecycleCategory);

    private void OnWindowOnStopped(object? sender, EventArgs _) =>
        _hub.AddBreadcrumbForEvent(_options, sender, nameof(Window.Stopped), SystemType, LifecycleCategory);

    private void OnWindowOnResumed(object? sender, EventArgs _) =>
        _hub.AddBreadcrumbForEvent(_options, sender, nameof(Window.Resumed), SystemType, LifecycleCategory);

    private void OnWindowOnCreated(object? sender, EventArgs _) =>
        _hub.AddBreadcrumbForEvent(_options, sender, nameof(Window.Created), SystemType, LifecycleCategory);

    private void OnWindowOnDestroying(object? sender, EventArgs _) =>
        _hub.AddBreadcrumbForEvent(_options, sender, nameof(Window.Destroying), SystemType, LifecycleCategory);

    private void OnWindowOnBackgrounding(object? sender, BackgroundingEventArgs e) =>
        _hub.AddBreadcrumbForEvent(_options, sender, nameof(Window.Backgrounding), SystemType, LifecycleCategory, data =>
        {
            if (!_options?.IncludeBackgroundingStateInBreadcrumbs ?? true)
            {
                return;
            }

            foreach (var item in e.State)
            {
                data.Add($"{nameof(e.State)}.{item.Key}", item.Value ?? "<null>");
            }
        });

    private void OnWindowOnDisplayDensityChanged(object? sender, DisplayDensityChangedEventArgs e) =>
        _hub.AddBreadcrumbForEvent(_options, sender, nameof(Window.DisplayDensityChanged), SystemType, LifecycleCategory, data =>
        {
            var displayDensity = e.DisplayDensity.ToString(CultureInfo.InvariantCulture);
            data.Add(nameof(e.DisplayDensity), displayDensity);
        });

    private void OnWindowOnModalPushed(object? sender, ModalPushedEventArgs e) =>
        _hub.AddBreadcrumbForEvent(_options, sender, nameof(Window.ModalPushed), NavigationType, NavigationCategory, data => data.AddElementInfo(_options, e.Modal, nameof(e.Modal)));

    private void OnWindowOnModalPopped(object? sender, ModalPoppedEventArgs e) =>
        _hub.AddBreadcrumbForEvent(_options, sender, nameof(Window.ModalPopped), NavigationType, NavigationCategory, data => data.AddElementInfo(_options, e.Modal, nameof(e.Modal)));

    private void OnWindowOnPopCanceled(object? sender, EventArgs _) =>
        _hub.AddBreadcrumbForEvent(_options, sender, nameof(Window.PopCanceled), NavigationType, NavigationCategory);

    // Element Events

    private void OnElementOnChildAdded(object? sender, ElementEventArgs e) =>
        _hub.AddBreadcrumbForEvent(_options, sender, nameof(Element.ChildAdded), SystemType, RenderingCategory, data => data.AddElementInfo(_options, e.Element, nameof(e.Element)));

    private void OnElementOnChildRemoved(object? sender, ElementEventArgs e) =>
        _hub.AddBreadcrumbForEvent(_options, sender, nameof(Element.ChildRemoved), SystemType, RenderingCategory, data => data.AddElementInfo(_options, e.Element, nameof(e.Element)));

    private void OnElementOnParentChanged(object? sender, EventArgs _) =>
        _hub.AddBreadcrumbForEvent(_options, sender, nameof(Element.ParentChanged), SystemType, RenderingCategory, data =>
        {
            var e = sender as Element;
            data.AddElementInfo(_options, e?.Parent, nameof(e.Parent));
        });

    private void OnElementOnBindingContextChanged(object? sender, EventArgs _)
    {
        if (sender is not BindableObject { BindingContext: { } bindingContext })
        {
            // Don't add breadcrumbs when BindingContext is null
            return;
        }

        var e = sender as Element;
        _hub.AddBreadcrumbForEvent(_options, e, nameof(BindableObject.BindingContextChanged), SystemType, RenderingCategory, data => data.Add(nameof(BindableObject.BindingContext), bindingContext.ToStringOrTypeName()));
    }

    // Visual Events

    private void OnElementOnFocused(object? sender, FocusEventArgs _) =>
        _hub.AddBreadcrumbForEvent(_options, sender, nameof(VisualElement.Focused), SystemType, RenderingCategory);

    private void OnElementOnUnfocused(object? sender, FocusEventArgs _) =>
        _hub.AddBreadcrumbForEvent(_options, sender, nameof(VisualElement.Unfocused), SystemType, RenderingCategory);

    // Shell Events

    private void OnShellOnNavigating(object? sender, ShellNavigatingEventArgs e) =>
        _hub.AddBreadcrumbForEvent(_options, sender, nameof(Shell.Navigating), NavigationType, NavigationCategory, data =>
        {
            data.Add("from", e.Current?.Location.ToString() ?? "");
            data.Add("to", e.Target?.Location.ToString() ?? "");
            data.Add(nameof(e.Source), e.Source.ToString());
        });

    private void OnShellOnNavigated(object? sender, ShellNavigatedEventArgs e) =>
        _hub.AddBreadcrumbForEvent(_options, sender, nameof(Shell.Navigated), NavigationType, NavigationCategory, data =>
        {
            data.Add("from", e.Previous?.Location.ToString() ?? "");
            data.Add("to", e.Current?.Location.ToString() ?? "");
            data.Add(nameof(e.Source), e.Source.ToString());
        });

    // Page Events

    private void OnPageOnAppearing(object? sender, EventArgs _) =>
        _hub.AddBreadcrumbForEvent(_options, sender, nameof(Page.Appearing), SystemType, LifecycleCategory);

    private void OnPageOnDisappearing(object? sender, EventArgs _) =>
        _hub.AddBreadcrumbForEvent(_options, sender, nameof(Page.Disappearing), SystemType, LifecycleCategory);

    private void OnPageOnNavigatedTo(object? sender, NavigatedToEventArgs e) =>
        _hub.AddBreadcrumbForEvent(_options, sender, nameof(Page.NavigatedTo), NavigationType, NavigationCategory, data => data.AddElementInfo(_options, e.GetPreviousPage(), "PreviousPage"));

    private void OnPageOnLayoutChanged(object? sender, EventArgs _) =>
        _hub.AddBreadcrumbForEvent(_options, sender, nameof(Page.LayoutChanged), SystemType, RenderingCategory);

    private void RunPageEventHandlers(Action<IMauiPageEventHandler> action)
    {
        foreach (var handler in _pageEventHandlers) action(handler); // TODO: try/catch in case of user code?
    }
}
