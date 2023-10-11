using Microsoft.Extensions.Options;
using Sentry.Extensibility;

namespace Sentry.Maui.Internal;

internal class MauiEventsBinder : IMauiEventsBinder
{
    private readonly IHub _hub;
    private readonly SentryMauiOptions _options;

    // https://develop.sentry.dev/sdk/event-payloads/breadcrumbs/#breadcrumb-types
    // https://github.com/getsentry/sentry/blob/master/static/app/types/breadcrumbs.tsx
    internal const string NavigationType = "navigation";
    internal const string SystemType = "system";
    internal const string UserType = "user";
    internal const string HandlersCategory = "ui.handlers";
    internal const string LifecycleCategory =  "ui.lifecycle";
    internal const string NavigationCategory = "navigation";
    internal const string RenderingCategory = "ui.rendering";
    internal const string UserActionCategory = "ui.useraction";

    // This list should contain all types that we have explicitly added handlers for their events.
    // Any elements that are not in this list will have their events discovered by reflection.
    internal static readonly HashSet<Type> ExplicitlyHandledTypes = new()
    {
        typeof(BindableObject),
        typeof(Element),
        typeof(VisualElement),
        typeof(Application),
        typeof(Window),
        typeof(Shell),
        typeof(Page),
        typeof(Button),
    };

    public MauiEventsBinder(IHub hub, IOptions<SentryMauiOptions> options)
    {
        _hub = hub;
        _options = options.Value;
    }

    public void BindApplicationEvents(Application application)
    {
        // Attach element events to all descendents as they are added to the application.
        application.DescendantAdded += (_, e) =>
        {
            // All elements have a set of common events we can hook
            BindElementEvents(e.Element);

            if (e.Element is VisualElement visualElement)
            {
                BindVisualElementEvents(visualElement);
            }

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
        application.PageAppearing += (sender, page) =>
            _hub.AddBreadcrumbForEvent(_options, sender, nameof(Application.PageAppearing), NavigationType, NavigationCategory,
                data => data.AddElementInfo(_options, page, nameof(Page)));
        application.PageDisappearing += (sender, page) =>
            _hub.AddBreadcrumbForEvent(_options, sender, nameof(Application.PageDisappearing), NavigationType, NavigationCategory,
                data => data.AddElementInfo(_options, page, nameof(Page)));
        application.ModalPushing += (sender, e) =>
            _hub.AddBreadcrumbForEvent(_options, sender, nameof(Application.ModalPushing), NavigationType, NavigationCategory,
                data => data.AddElementInfo(_options, e.Modal, nameof(e.Modal)));
        application.ModalPushed += (sender, e) =>
            _hub.AddBreadcrumbForEvent(_options, sender, nameof(Application.ModalPushed), NavigationType, NavigationCategory,
                data => data.AddElementInfo(_options, e.Modal, nameof(e.Modal)));
        application.ModalPopping += (sender, e) =>
            _hub.AddBreadcrumbForEvent(_options, sender, nameof(Application.ModalPopping), NavigationType, NavigationCategory,
                data => data.AddElementInfo(_options, e.Modal, nameof(e.Modal)));
        application.ModalPopped += (sender, e) =>
            _hub.AddBreadcrumbForEvent(_options, sender, nameof(Application.ModalPopped), NavigationType, NavigationCategory,
                data => data.AddElementInfo(_options, e.Modal, nameof(e.Modal)));

        // Theme changed event
        // https://docs.microsoft.com/dotnet/maui/user-interface/system-theme-changes#react-to-theme-changes
        application.RequestedThemeChanged += (sender, e) =>
            _hub.AddBreadcrumbForEvent(_options, sender, nameof(Application.RequestedThemeChanged), SystemType, RenderingCategory,
                data => data.Add(nameof(e.RequestedTheme), e.RequestedTheme.ToString()));
    }

    public void BindReflectedEvents(BindableObject bindableObject, bool includeExplicitlyHandledTypes = false)
    {
        // This reflects over the object's events.
        // By default, it attaches only to events that are *NOT* declared by types in the ExplicitlyHandledTypes list.
        // We will only include such events when testing.

        var type = bindableObject.GetType();

        IEnumerable<EventInfo> events = type.GetEvents(BindingFlags.Instance | BindingFlags.Public);
        if (!includeExplicitlyHandledTypes)
        {
            events = events.Where(e => !ExplicitlyHandledTypes.Contains(e.DeclaringType!));
        }

        foreach (var eventInfo in events)
        {
            var browsable = eventInfo.GetCustomAttribute<EditorBrowsableAttribute>();
            if (browsable != null && browsable.State != EditorBrowsableState.Always)
            {
                // These events are not meant for typical consumption.
                continue;
            }

            Action<object, object> handler = (sender, _) =>
            {
                _hub.AddBreadcrumbForEvent(_options, sender, eventInfo.Name);
            };

            try
            {
                var typedHandler = Delegate.CreateDelegate(eventInfo.EventHandlerType!, handler.Target, handler.Method);
                eventInfo.AddEventHandler(bindableObject, typedHandler);
            }
            catch (Exception ex)
            {
                // Don't throw if we can't bind the event handler
                _options.DiagnosticLogger?.LogError(ex, "Couldn't bind to {0}.{1}", type.Name, eventInfo.Name);
            }
        }
    }

    public void BindWindowEvents(Window window)
    {
        // Lifecycle Events
        // https://docs.microsoft.com/dotnet/maui/fundamentals/windows
        // https://docs.microsoft.com/dotnet/maui/fundamentals/app-lifecycle#cross-platform-lifecycle-events

        // Lifecycle events caused by user action
        window.Activated += (sender, _) =>
            _hub.AddBreadcrumbForEvent(_options, sender, nameof(Window.Activated), SystemType, LifecycleCategory);
        window.Deactivated += (sender, _) =>
            _hub.AddBreadcrumbForEvent(_options, sender, nameof(Window.Deactivated), SystemType, LifecycleCategory);
        window.Stopped += (sender, _) =>
            _hub.AddBreadcrumbForEvent(_options, sender, nameof(Window.Stopped), SystemType, LifecycleCategory);
        window.Resumed += (sender, _) =>
            _hub.AddBreadcrumbForEvent(_options, sender, nameof(Window.Resumed), SystemType, LifecycleCategory);

        // System generated lifecycle events
        window.Created += (sender, _) =>
            _hub.AddBreadcrumbForEvent(_options, sender, nameof(Window.Created), SystemType, LifecycleCategory);
        window.Destroying += (sender, _) =>
            _hub.AddBreadcrumbForEvent(_options, sender, nameof(Window.Destroying), SystemType, LifecycleCategory);
        window.Backgrounding += (sender, e) =>
            _hub.AddBreadcrumbForEvent(_options, sender, nameof(Window.Backgrounding), SystemType, LifecycleCategory,
                data =>
                {
                    if (!_options.IncludeBackgroundingStateInBreadcrumbs)
                    {
                        return;
                    }

                    foreach (var item in e.State)
                    {
                        data.Add($"{nameof(e.State)}.{item.Key}", item.Value ?? "<null>");
                    }
                });
        window.DisplayDensityChanged += (sender, e) =>
            _hub.AddBreadcrumbForEvent(_options, sender, nameof(Window.DisplayDensityChanged), SystemType, LifecycleCategory,
                data =>
                {
                    var displayDensity = e.DisplayDensity.ToString(CultureInfo.InvariantCulture);
                    data.Add(nameof(e.DisplayDensity), displayDensity);
                });

        // Navigation events
        window.ModalPushing += (sender, e) =>
            _hub.AddBreadcrumbForEvent(_options, sender, nameof(Window.ModalPushing), NavigationType, NavigationCategory,
                data => data.AddElementInfo(_options, e.Modal, nameof(e.Modal)));
        window.ModalPushed += (sender, e) =>
            _hub.AddBreadcrumbForEvent(_options, sender, nameof(Window.ModalPushed), NavigationType, NavigationCategory,
                data => data.AddElementInfo(_options, e.Modal, nameof(e.Modal)));
        window.ModalPopping += (sender, e) =>
            _hub.AddBreadcrumbForEvent(_options, sender, nameof(Window.ModalPopping), NavigationType, NavigationCategory,
                data => data.AddElementInfo(_options, e.Modal, nameof(e.Modal)));
        window.ModalPopped += (sender, e) =>
            _hub.AddBreadcrumbForEvent(_options, sender, nameof(Window.ModalPopped), NavigationType, NavigationCategory,
                data => data.AddElementInfo(_options, e.Modal, nameof(e.Modal)));
        window.PopCanceled += (sender, _) =>
            _hub.AddBreadcrumbForEvent(_options, sender, nameof(Window.PopCanceled), NavigationType, NavigationCategory);
    }

    public void BindElementEvents(Element element)
    {
        // Element handler events
        // https://docs.microsoft.com/dotnet/maui/user-interface/handlers/customize#handler-lifecycle
        element.HandlerChanging += (sender, e) =>
            _hub.AddBreadcrumbForEvent(_options, sender, nameof(Element.HandlerChanging), SystemType, HandlersCategory,
                data =>
                {
                    data.Add(nameof(e.OldHandler), e.OldHandler?.ToStringOrTypeName() ?? "");
                    data.Add(nameof(e.NewHandler), e.NewHandler?.ToStringOrTypeName() ?? "");
                });
        element.HandlerChanged += (sender, _) =>
            _hub.AddBreadcrumbForEvent(_options, sender, nameof(Element.HandlerChanged), SystemType, HandlersCategory,
                data =>
                {
                    var e = sender as Element;
                    data.Add(nameof(e.Handler), e?.Handler?.ToStringOrTypeName() ?? "");
                });

        // Rendering events
        element.ChildAdded += (sender, e) =>
            _hub.AddBreadcrumbForEvent(_options, sender, nameof(Element.ChildAdded), SystemType, RenderingCategory,
                data => data.AddElementInfo(_options, e.Element, nameof(e.Element)));
        element.ChildRemoved += (sender, e) =>
            _hub.AddBreadcrumbForEvent(_options, sender, nameof(Element.ChildRemoved), SystemType, RenderingCategory,
                data => data.AddElementInfo(_options, e.Element, nameof(e.Element)));
        element.ParentChanging += (sender, e) =>
            _hub.AddBreadcrumbForEvent(_options, sender, nameof(Element.ParentChanging), SystemType, RenderingCategory,
                data =>
                {
                    data.AddElementInfo(_options, e.OldParent, nameof(e.OldParent));
                    data.AddElementInfo(_options, e.NewParent, nameof(e.NewParent));
                });
        element.ParentChanged += (sender, _) =>
            _hub.AddBreadcrumbForEvent(_options, sender, nameof(Element.ParentChanged), SystemType, RenderingCategory,
                data =>
                {
                    var e = sender as Element;
                    data.AddElementInfo(_options, e?.Parent, nameof(e.Parent));
                });

        // These lead to lots of duplicate information, so probably best not to include them.
        // element.DescendantAdded
        // element.DescendantRemoved

        // BindableObject events
        element.BindingContextChanged += (sender, _) =>
        {
            if (sender is not BindableObject {BindingContext: { } bindingContext})
            {
                // Don't add breadcrumbs when BindingContext is null
                return;
            }

            _hub.AddBreadcrumbForEvent(
                _options,
                element,
                nameof(BindableObject.BindingContextChanged),
                SystemType,
                RenderingCategory,
                data =>
                    data.Add(nameof(BindableObject.BindingContext), bindingContext.ToStringOrTypeName()));
        };

        // NotifyPropertyChanged events are too noisy to be useful
        // element.PropertyChanging
        // element.PropertyChanged
    }

    public void BindVisualElementEvents(VisualElement element)
    {
        element.Focused += (sender, _) =>
            _hub.AddBreadcrumbForEvent(_options, sender, nameof(VisualElement.Focused), SystemType, RenderingCategory);

        element.Unfocused += (sender, _) =>
            _hub.AddBreadcrumbForEvent(_options, sender, nameof(VisualElement.Unfocused), SystemType, RenderingCategory);

        element.Loaded += (sender, _) =>
            _hub.AddBreadcrumbForEvent(_options, sender, nameof(VisualElement.Loaded), SystemType, RenderingCategory);

        element.Unloaded += (sender, _) =>
            _hub.AddBreadcrumbForEvent(_options, sender, nameof(VisualElement.Unloaded), SystemType, RenderingCategory);

        element.ChildrenReordered += (sender, _) =>
            _hub.AddBreadcrumbForEvent(_options, sender, nameof(VisualElement.ChildrenReordered), SystemType, RenderingCategory);

        element.MeasureInvalidated += (sender, _) =>
            _hub.AddBreadcrumbForEvent(_options, sender, nameof(VisualElement.MeasureInvalidated), SystemType, RenderingCategory);

        element.SizeChanged += (sender, _) =>
            _hub.AddBreadcrumbForEvent(_options, sender, nameof(VisualElement.SizeChanged), SystemType, RenderingCategory);
    }

    public void BindShellEvents(Shell shell)
    {
        // Navigation events
        // https://docs.microsoft.com/dotnet/maui/fundamentals/shell/navigation
        shell.Navigating += (sender, e) =>
            _hub.AddBreadcrumbForEvent(_options, sender, nameof(Shell.Navigating), NavigationType, NavigationCategory,
                data =>
                {
                    data.Add("from", e.Current?.Location.ToString() ?? "");
                    data.Add("to", e.Target?.Location.ToString() ?? "");
                    data.Add(nameof(e.Source), e.Source.ToString());
                });
        shell.Navigated += (sender, e) =>
            _hub.AddBreadcrumbForEvent(_options, sender, nameof(Shell.Navigated), NavigationType, NavigationCategory,
                data =>
                {
                    data.Add("from", e.Previous?.Location.ToString() ?? "");
                    data.Add("to", e.Current?.Location.ToString() ?? "");
                    data.Add(nameof(e.Source), e.Source.ToString());
                });

        // A Shell is also a Page
        BindPageEvents(shell);
    }

    public void BindPageEvents(Page page)
    {
        // Lifecycle events
        // https://docs.microsoft.com/dotnet/maui/fundamentals/shell/lifecycle
        page.Appearing += (sender, _) =>
            _hub.AddBreadcrumbForEvent(_options, sender, nameof(Page.Appearing), SystemType, LifecycleCategory);
        page.Disappearing += (sender, _) =>
            _hub.AddBreadcrumbForEvent(_options, sender, nameof(Page.Disappearing), SystemType, LifecycleCategory);

        // Navigation events
        // https://github.com/dotnet/docs-maui/issues/583
        page.NavigatingFrom += (sender, _) =>
            _hub.AddBreadcrumbForEvent(_options, sender, nameof(Page.NavigatingFrom), NavigationType, NavigationCategory);
        page.NavigatedFrom += (sender, e) =>
            _hub.AddBreadcrumbForEvent(_options, sender, nameof(Page.NavigatedFrom), NavigationType, NavigationCategory,
                data => data.AddElementInfo(_options, e.GetDestinationPage(), "DestinationPage"));
        page.NavigatedTo += (sender, e) =>
            _hub.AddBreadcrumbForEvent(_options, sender, nameof(Page.NavigatedTo), NavigationType, NavigationCategory,
                data => data.AddElementInfo(_options, e.GetPreviousPage(), "PreviousPage"));

        // Layout changed event
        // https://docs.microsoft.com/dotnet/api/xamarin.forms.ilayout.layoutchanged
        page.LayoutChanged += (sender, _) =>
            _hub.AddBreadcrumbForEvent(_options, sender, nameof(Page.LayoutChanged), SystemType, RenderingCategory);
    }

    public void BindButtonEvents(Button button)
    {
        button.Clicked += (sender, _) =>
            _hub.AddBreadcrumbForEvent(_options, sender, nameof(Button.Clicked), UserType, UserActionCategory);
        button.Pressed += (sender, _) =>
            _hub.AddBreadcrumbForEvent(_options, sender, nameof(Button.Pressed), UserType, UserActionCategory);
        button.Released += (sender, _) =>
            _hub.AddBreadcrumbForEvent(_options, sender, nameof(Button.Released), UserType, UserActionCategory);
    }
}
