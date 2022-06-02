using System.Globalization;
using System.Reflection;
using Microsoft.Extensions.Options;
using Sentry.Extensibility;

namespace Sentry.Maui.Internal;

internal class MauiEventsBinder : IMauiEventsBinder
{
    private readonly IApplication _application;
    private readonly IHub _hub;
    private readonly SentryMauiOptions _options;

    // https://develop.sentry.dev/sdk/event-payloads/breadcrumbs/#breadcrumb-types
    // https://github.com/getsentry/sentry/blob/master/static/app/types/breadcrumbs.tsx
    private const string NavigationType = "navigation";
    private const string SystemType = "system";
    private const string UserType = "user";
    private const string HandlersCategory = "ui.handlers";
    private const string LifecycleCategory =  "ui.lifecycle";
    private const string NavigationCategory = "navigation";
    private const string RenderingCategory = "ui.rendering";
    private const string UserActionCategory = "ui.useraction";

    // This list should contain all types that we have explicitly added handlers for their events.
    // Any elements that are not in this list will have their events discovered by reflection.
    private static readonly HashSet<Type> ExplicitlyHandledTypes = new()
    {
        typeof(Element),
        typeof(BindableObject),
        typeof(Application),
        typeof(Window),
        typeof(Shell),
        typeof(Page),
        typeof(Button),
    };

    public MauiEventsBinder(IApplication application, IHub hub, IOptions<SentryMauiOptions> options)
    {
        _application = application;
        _hub = hub;
        _options = options.Value;
    }

    public void BindMauiEvents()
    {
        // Bind to the MAUI application events in a real application (not when testing)
        if (_application is not Application application)
        {
            return;
        }

        BindApplicationEvents(application);
    }

    private void BindApplicationEvents(Application application)
    {
        // Attach element events to all descendents as they are added to the application.
        application.DescendantAdded += (_, e) =>
        {
            // All elements have a set of common events we can hook
            BindElementEvents(e.Element);

            // We'll use reflection to attach to other events
            // This allows us to attach to events from custom controls
            BindReflectedEvents(e.Element);

            // We can also attach to specific events on built-in controls
            // Be sure to update ExplicitlyHandledTypes when adding to this list
            switch (e.Element)
            {
                case Window window:
                    BindWindowEvents(window);
                    break;
                case Shell shell:
                    BindShellEvents(shell);
                    break;
                case Page page:
                    BindPageEvents(page);
                    break;
                case Button button:
                    BindButtonEvents(button);
                    break;

                // TODO: Attach to specific events on more control types
            }
        };

        // The application is an element itself, so attach element events here also.
        BindElementEvents(application);

        // Navigation events
        application.ModalPopping += (sender, e) =>
            _hub.AddBreadcrumbForEvent(sender, nameof(Application.ModalPopping), NavigationType, NavigationCategory,
                data => data.AddElementInfo(e.Modal, nameof(e.Modal)));
        application.ModalPopped += (sender, e) =>
            _hub.AddBreadcrumbForEvent(sender, nameof(Application.ModalPopped), NavigationType, NavigationCategory,
                data => data.AddElementInfo(e.Modal, nameof(e.Modal)));
        application.ModalPushing += (sender, e) =>
            _hub.AddBreadcrumbForEvent(sender, nameof(Application.ModalPushing), NavigationType, NavigationCategory,
                data => data.AddElementInfo(e.Modal, nameof(e.Modal)));
        application.ModalPushed += (sender, e) =>
            _hub.AddBreadcrumbForEvent(sender, nameof(Application.ModalPushed), NavigationType, NavigationCategory,
                data => data.AddElementInfo(e.Modal, nameof(e.Modal)));
        application.PageAppearing += (sender, page) =>
            _hub.AddBreadcrumbForEvent(sender, nameof(Application.PageAppearing), NavigationType, NavigationCategory,
                data => data.AddElementInfo(page, nameof(Page)));
        application.PageDisappearing += (sender, page) =>
            _hub.AddBreadcrumbForEvent(sender, nameof(Application.PageDisappearing), NavigationType, NavigationCategory,
                data => data.AddElementInfo(page, nameof(Page)));

        // Theme changed event
        // https://docs.microsoft.com/dotnet/maui/user-interface/system-theme-changes#react-to-theme-changes
        application.RequestedThemeChanged += (sender, e) =>
            _hub.AddBreadcrumbForEvent(sender, nameof(Application.RequestedThemeChanged), SystemType, RenderingCategory,
                data => data.Add(nameof(e.RequestedTheme), e.RequestedTheme.ToString()));
    }

    private void BindReflectedEvents(Element element)
    {
        // This reflects over the element's events, and attaches to any that
        // are *NOT* declared by types in the ExplicitlyHandledTypes list.

        var elementType = element.GetType();
        var events = elementType.GetEvents(BindingFlags.Instance | BindingFlags.Public);
        foreach (var eventInfo in events.Where(e => !ExplicitlyHandledTypes.Contains(e.DeclaringType!)))
        {
            Action<object, object> handler = (sender, _) =>
            {
                _hub.AddBreadcrumbForEvent(sender, eventInfo.Name);
            };

            try
            {
                var typedHandler = Delegate.CreateDelegate(eventInfo.EventHandlerType!, handler.Target, handler.Method);
                eventInfo.AddEventHandler(element, typedHandler);
            }
            catch (Exception ex)
            {
                // Don't throw if we can't bind the event handler
                _options.DiagnosticLogger?.LogError("Couldn't bind to {0}.{1}", ex, elementType.Name, eventInfo.Name);
            }
        }
    }

    private void BindWindowEvents(Window window)
    {
        // Lifecycle Events
        // https://docs.microsoft.com/dotnet/maui/fundamentals/windows
        // https://docs.microsoft.com/dotnet/maui/fundamentals/app-lifecycle#cross-platform-lifecycle-events

        // Lifecycle events caused by user action
        window.Activated += (sender, _) =>
            _hub.AddBreadcrumbForEvent(sender, nameof(Window.Activated), SystemType, LifecycleCategory);
        window.Deactivated += (sender, _) =>
            _hub.AddBreadcrumbForEvent(sender, nameof(Window.Deactivated), SystemType, LifecycleCategory);
        window.Stopped += (sender, _) =>
            _hub.AddBreadcrumbForEvent(sender, nameof(Window.Stopped), SystemType, LifecycleCategory);
        window.Resumed += (sender, _) =>
            _hub.AddBreadcrumbForEvent(sender, nameof(Window.Resumed), SystemType, LifecycleCategory);

        // System generated lifecycle events
        window.Created += (sender, _) =>
            _hub.AddBreadcrumbForEvent(sender, nameof(Window.Created), SystemType, LifecycleCategory);
        window.Destroying += (sender, _) =>
            _hub.AddBreadcrumbForEvent(sender, nameof(Window.Destroying), SystemType, LifecycleCategory);
        window.Backgrounding += (sender, e) =>
            _hub.AddBreadcrumbForEvent(sender, nameof(Window.Destroying), SystemType, LifecycleCategory,
                data =>
                {
                    // TODO: Could this contain PII?
                    foreach (var item in e.State)
                    {
                        data.Add($"{nameof(e.State)}.{item.Key}", item.Value ?? "<null>");
                    }
                });
        window.DisplayDensityChanged += (sender, e) =>
            _hub.AddBreadcrumbForEvent(sender, nameof(Window.Destroying), SystemType, LifecycleCategory,
                data =>
                {
                    var displayDensity = e.DisplayDensity.ToString(CultureInfo.InvariantCulture);
                    data.Add(nameof(e.DisplayDensity), displayDensity);
                });

        // Navigation events
        window.ModalPopping += (sender, e) =>
            _hub.AddBreadcrumbForEvent(sender, nameof(Window.ModalPopping), NavigationType, NavigationCategory,
                data => data.AddElementInfo(e.Modal, nameof(e.Modal)));
        window.ModalPopped += (sender, e) =>
            _hub.AddBreadcrumbForEvent(sender, nameof(Window.ModalPopped), NavigationType, NavigationCategory,
                data => data.AddElementInfo(e.Modal, nameof(e.Modal)));
        window.ModalPushing += (sender, e) =>
            _hub.AddBreadcrumbForEvent(sender, nameof(Window.ModalPushing), NavigationType, NavigationCategory,
                data => data.AddElementInfo(e.Modal, nameof(e.Modal)));
        window.ModalPushed += (sender, e) =>
            _hub.AddBreadcrumbForEvent(sender, nameof(Window.ModalPushed), NavigationType, NavigationCategory,
                data => data.AddElementInfo(e.Modal, nameof(e.Modal)));
        window.PopCanceled += (sender, _) =>
            _hub.AddBreadcrumbForEvent(sender, nameof(Window.PopCanceled), NavigationType, NavigationCategory);
    }

    private void BindElementEvents(Element element)
    {
        // Element handler events
        // https://docs.microsoft.com/dotnet/maui/user-interface/handlers/customize#handler-lifecycle
        element.HandlerChanging += (sender, e) =>
            _hub.AddBreadcrumbForEvent(sender, nameof(Window.HandlerChanging), SystemType, HandlersCategory,
                data =>
                {
                    data.Add(nameof(e.OldHandler), e.OldHandler?.ToString() ?? "");
                    data.Add(nameof(e.NewHandler), e.NewHandler?.ToString() ?? "");
                });
        element.HandlerChanged += (sender, _) =>
            _hub.AddBreadcrumbForEvent(sender, nameof(Window.HandlerChanged), SystemType, HandlersCategory);

        // Rendering events
        element.ChildAdded += (sender, e) =>
            _hub.AddBreadcrumbForEvent(sender, nameof(Window.ChildAdded), SystemType, RenderingCategory,
                data => data.AddElementInfo(e.Element, nameof(e.Element)));
        element.ChildRemoved += (sender, e) =>
            _hub.AddBreadcrumbForEvent(sender, nameof(Window.ChildRemoved), SystemType, RenderingCategory,
                data => data.AddElementInfo(e.Element, nameof(e.Element)));
        element.ParentChanging += (sender, e) =>
            _hub.AddBreadcrumbForEvent(sender, nameof(Window.ParentChanging), SystemType, RenderingCategory,
                data =>
                {
                    data.AddElementInfo(e.OldParent, nameof(e.OldParent));
                    data.AddElementInfo(e.NewParent, nameof(e.NewParent));
                });
        element.ParentChanged += (sender, _) =>
            _hub.AddBreadcrumbForEvent(sender, nameof(Window.ParentChanged), SystemType, RenderingCategory);

        // These lead to lots of duplicate information, so probably best not to include them.
        // element.DescendantAdded
        // element.DescendantRemoved

        // BindableObject events
        element.BindingContextChanged += (sender, _) =>
        {
            var bo = (BindableObject)sender!;
            if (bo.BindingContext != null)
            {
                // Don't add breadcrumbs when this is null
                _hub.AddBreadcrumbForEvent(element, nameof(bo.BindingContextChanged), SystemType, RenderingCategory,
                    data => data.Add(nameof(bo.BindingContext), bo.BindingContext.GetType().Name));
            }
        };

        // NotifyPropertyChanged events are too noisy to be useful
        // element.PropertyChanging
        // element.PropertyChanged
    }

    private void BindShellEvents(Shell shell)
    {
        // Navigation events
        // https://docs.microsoft.com/dotnet/maui/fundamentals/shell/navigation
        shell.Navigating += (sender, e) =>
            _hub.AddBreadcrumbForEvent(sender, nameof(Shell.Navigating), NavigationType, NavigationCategory,
                data =>
                {
                    data.Add("from", e.Current?.Location.ToString() ?? "");
                    data.Add("to", e.Target?.Location.ToString() ?? "");
                    data.Add(nameof(e.Source), e.Source.ToString());
                });
        shell.Navigated += (sender, e) =>
            _hub.AddBreadcrumbForEvent(sender, nameof(Shell.Navigated), NavigationType, NavigationCategory,
                data =>
                {
                    data.Add("from", e.Previous?.Location.ToString() ?? "");
                    data.Add("to", e.Current?.Location.ToString() ?? "");
                    data.Add(nameof(e.Source), e.Source.ToString());
                });

        // A Shell is also a Page
        BindPageEvents(shell);
    }

    private void BindPageEvents(Page page)
    {
        // Lifecycle events
        // https://docs.microsoft.com/dotnet/maui/fundamentals/shell/lifecycle
        page.Appearing += (sender, _) =>
            _hub.AddBreadcrumbForEvent(sender, nameof(Page.Appearing), SystemType, RenderingCategory);
        page.Disappearing += (sender, _) =>
            _hub.AddBreadcrumbForEvent(sender, nameof(Page.Disappearing), SystemType, RenderingCategory);

        // Navigation events
        // https://github.com/dotnet/docs-maui/issues/583
        page.NavigatingFrom += (sender, _) =>
            _hub.AddBreadcrumbForEvent(sender, nameof(Page.NavigatingFrom), NavigationType, NavigationCategory);
        page.NavigatedFrom += (sender, _) =>
            _hub.AddBreadcrumbForEvent(sender, nameof(Page.NavigatedFrom), NavigationType, NavigationCategory);
        page.NavigatedTo += (sender, _) =>
            _hub.AddBreadcrumbForEvent(sender, nameof(Page.NavigatedTo), NavigationType, NavigationCategory);

        // Layout changed event
        // https://docs.microsoft.com/dotnet/api/xamarin.forms.ilayout.layoutchanged
        page.LayoutChanged += (sender, _) =>
            _hub.AddBreadcrumbForEvent(sender, nameof(Page.LayoutChanged), SystemType, RenderingCategory);
    }

    private void BindButtonEvents(Button button)
    {
        button.Clicked += (sender, _) =>
            _hub.AddBreadcrumbForEvent(sender, nameof(Button.Clicked), UserType, UserActionCategory);
        button.Pressed += (sender, _) =>
            _hub.AddBreadcrumbForEvent(sender, nameof(Button.Pressed), UserType,UserActionCategory);
        button.Released += (sender, _) =>
            _hub.AddBreadcrumbForEvent(sender, nameof(Button.Released), UserType,UserActionCategory);
    }
}
