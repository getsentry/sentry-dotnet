using Microsoft.Extensions.Options;

namespace Sentry.Maui.Internal;

internal class MauiEventsBinder : IMauiEventsBinder
{
    private readonly SentryMauiOptions _options;

    public MauiEventsBinder(IHub hub, IOptions<SentryMauiOptions> options)
    {
        _options = options.Value;

        MauiEvents.Hub = hub;
        MauiEvents.Options = _options;
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
            application.PageAppearing += MauiEvents.OnApplicationOnPageAppearing;
            application.PageDisappearing += MauiEvents.OnApplicationOnPageDisappearing;
            application.ModalPushed += MauiEvents.OnApplicationOnModalPushed;
            application.ModalPopped += MauiEvents.OnApplicationOnModalPopped;

            // Theme changed event
            // https://docs.microsoft.com/dotnet/maui/user-interface/system-theme-changes#react-to-theme-changes
            application.RequestedThemeChanged += MauiEvents.OnApplicationOnRequestedThemeChanged;
        }
        else
        {
            application.DescendantAdded -= OnApplicationOnDescendantAdded;
            application.DescendantRemoved -= OnApplicationOnDescendantRemoved;

            HandleElementEvents(application, bind: false);

            // Navigation events
            application.PageAppearing -= MauiEvents.OnApplicationOnPageAppearing;
            application.PageDisappearing -= MauiEvents.OnApplicationOnPageDisappearing;
            application.ModalPushed -= MauiEvents.OnApplicationOnModalPushed;
            application.ModalPopped -= MauiEvents.OnApplicationOnModalPopped;

            // Theme changed event
            application.RequestedThemeChanged -= MauiEvents.OnApplicationOnRequestedThemeChanged;
        }
    }

    private void OnApplicationOnDescendantAdded(object? _, ElementEventArgs e)
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
            case Button button:
                HandleButtonEvents(button);
                break;

            // TODO: Attach to specific events on more control types
        }
    }

    private void OnApplicationOnDescendantRemoved(object? _, ElementEventArgs e)
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
            case Button button:
                HandleButtonEvents(button, bind: false);
                break;
        }
    }

    private static void HandleWindowEvents(Window window, bool bind = true)
    {
        if(bind)
        {
            // Lifecycle Events
            // https://docs.microsoft.com/dotnet/maui/fundamentals/windows
            // https://docs.microsoft.com/dotnet/maui/fundamentals/app-lifecycle#cross-platform-lifecycle-events

            // Lifecycle events caused by user action
            window.Activated += MauiEvents.OnWindowOnActivated;
            window.Deactivated += MauiEvents.OnWindowOnDeactivated;
            window.Stopped += MauiEvents.OnWindowOnStopped;
            window.Resumed += MauiEvents.OnWindowOnResumed;

            // System generated lifecycle events
            window.Created += MauiEvents.OnWindowOnCreated;
            window.Destroying += MauiEvents.OnWindowOnDestroying;
            window.Backgrounding += MauiEvents.OnWindowOnBackgrounding;
            window.DisplayDensityChanged += MauiEvents.OnWindowOnDisplayDensityChanged;

            // Navigation events
            window.ModalPushed += MauiEvents.OnWindowOnModalPushed;
            window.ModalPopped += MauiEvents.OnWindowOnModalPopped;
            window.PopCanceled += MauiEvents.OnWindowOnPopCanceled;
        }
        else
        {
            // Lifecycle events caused by user action
            window.Activated -= MauiEvents.OnWindowOnActivated;
            window.Deactivated -= MauiEvents.OnWindowOnDeactivated;
            window.Stopped -= MauiEvents.OnWindowOnStopped;
            window.Resumed -= MauiEvents.OnWindowOnResumed;

            // System generated lifecycle events
            window.Created -= MauiEvents.OnWindowOnCreated;
            window.Destroying -= MauiEvents.OnWindowOnDestroying;
            window.Backgrounding -= MauiEvents.OnWindowOnBackgrounding;
            window.DisplayDensityChanged -= MauiEvents.OnWindowOnDisplayDensityChanged;

            // Navigation events
            window.ModalPushed -= MauiEvents.OnWindowOnModalPushed;
            window.ModalPopped -= MauiEvents.OnWindowOnModalPopped;
            window.PopCanceled -= MauiEvents.OnWindowOnPopCanceled;
        }
    }

    private static void HandleElementEvents(Element element, bool bind = true)
    {
        if (bind)
        {
            // Rendering events
            element.ChildAdded += MauiEvents.OnElementOnChildAdded;
            element.ChildRemoved += MauiEvents.OnElementOnChildRemoved;
            element.ParentChanged += MauiEvents.OnElementOnParentChanged;

            // These lead to lots of duplicate information, so probably best not to include them.
            // element.DescendantAdded
            // element.DescendantRemoved

            // BindableObject events
            element.BindingContextChanged += MauiEvents.OnElementOnBindingContextChanged;
        }
        else
        {
            // Rendering events
            element.ChildAdded -= MauiEvents.OnElementOnChildAdded;
            element.ChildRemoved -= MauiEvents.OnElementOnChildRemoved;
            element.ParentChanged -= MauiEvents.OnElementOnParentChanged;

            // BindableObject events
            element.BindingContextChanged -= MauiEvents.OnElementOnBindingContextChanged;
        }
    }

    private static void HandleVisualElementEvents(VisualElement element, bool bind = true)
    {
        if (bind)
        {
            element.Focused += MauiEvents.OnElementOnFocused;
            element.Unfocused += MauiEvents.OnElementOnUnfocused;
        }
        else
        {
            element.Focused -= MauiEvents.OnElementOnFocused;
            element.Unfocused -= MauiEvents.OnElementOnUnfocused;
        }
    }

    private static void HandleShellEvents(Shell shell, bool bind = true)
    {
        if (bind)
        {
            // Navigation events
            // https://docs.microsoft.com/dotnet/maui/fundamentals/shell/navigation
            shell.Navigating += MauiEvents.OnShellOnNavigating;
            shell.Navigated += MauiEvents.OnShellOnNavigated;

            // A Shell is also a Page
            HandlePageEvents(shell);
        }
        else
        {
            // Navigation events
            // https://docs.microsoft.com/dotnet/maui/fundamentals/shell/navigation
            shell.Navigating -= MauiEvents.OnShellOnNavigating;
            shell.Navigated -= MauiEvents.OnShellOnNavigated;

            // A Shell is also a Page
            HandlePageEvents(shell, bind: false);
        }
    }

    private static void HandlePageEvents(Page page, bool bind = true)
    {
        if (bind)
        {
            // Lifecycle events
            // https://docs.microsoft.com/dotnet/maui/fundamentals/shell/lifecycle
            page.Appearing += MauiEvents.OnPageOnAppearing;
            page.Disappearing += MauiEvents.OnPageOnDisappearing;

            // Navigation events
            // https://github.com/dotnet/docs-maui/issues/583
            page.NavigatedTo += MauiEvents.OnPageOnNavigatedTo;

            // Layout changed event
            // https://docs.microsoft.com/dotnet/api/xamarin.forms.ilayout.layoutchanged
            page.LayoutChanged += MauiEvents.OnPageOnLayoutChanged;
        }
        else
        {
            // Lifecycle events
            // https://docs.microsoft.com/dotnet/maui/fundamentals/shell/lifecycle
            page.Appearing -= MauiEvents.OnPageOnAppearing;
            page.Disappearing -= MauiEvents.OnPageOnDisappearing;

            // Navigation events
            // https://github.com/dotnet/docs-maui/issues/583
            page.NavigatedTo -= MauiEvents.OnPageOnNavigatedTo;

            // Layout changed event
            // https://docs.microsoft.com/dotnet/api/xamarin.forms.ilayout.layoutchanged
            page.LayoutChanged -= MauiEvents.OnPageOnLayoutChanged;
        }
    }

    private static void HandleButtonEvents(Button button, bool bind = true)
    {
        if (bind)
        {
            button.Clicked += MauiEvents.OnButtonOnClicked;
            button.Pressed += MauiEvents.OnButtonOnPressed;
            button.Released += MauiEvents.OnButtonOnReleased;
        }
        else
        {
            button.Clicked -= MauiEvents.OnButtonOnClicked;
            button.Pressed -= MauiEvents.OnButtonOnPressed;
            button.Released -= MauiEvents.OnButtonOnReleased;
        }
    }
}
