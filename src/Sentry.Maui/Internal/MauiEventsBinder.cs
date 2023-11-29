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

    public void BindApplicationEvents(Application application)
    {
        // Attach element events to all descendents as they are added to the application.
        application.DescendantAdded += OnApplicationOnDescendantAdded;
        application.DescendantRemoved += OnApplicationOnDescendantRemoved;

        // The application is an element itself, so attach element events here also.
        BindElementEvents(application);

        // Navigation events
        application.PageAppearing += MauiEvents.OnApplicationOnPageAppearing;
        application.PageDisappearing += MauiEvents.OnApplicationOnPageDisappearing;
        application.ModalPushed += MauiEvents.OnApplicationOnModalPushed;
        application.ModalPopped += MauiEvents.OnApplicationOnModalPopped;

        // Theme changed event
        // https://docs.microsoft.com/dotnet/maui/user-interface/system-theme-changes#react-to-theme-changes
        application.RequestedThemeChanged += MauiEvents.OnApplicationOnRequestedThemeChanged;
    }

    public void UnbindApplicationEvents(Application application)
    {
        application.DescendantAdded -= OnApplicationOnDescendantAdded;
        application.DescendantRemoved -= OnApplicationOnDescendantRemoved;

        UnbindElementEvents(application);

        // Navigation events
        application.PageAppearing -= MauiEvents.OnApplicationOnPageAppearing;
        application.PageDisappearing -= MauiEvents.OnApplicationOnPageDisappearing;
        application.ModalPushed -= MauiEvents.OnApplicationOnModalPushed;
        application.ModalPopped -= MauiEvents.OnApplicationOnModalPopped;

        // Theme changed event
        application.RequestedThemeChanged -= MauiEvents.OnApplicationOnRequestedThemeChanged;
    }

    private void OnApplicationOnDescendantAdded(object? _, ElementEventArgs e)
    {
        // All elements have a set of common events we can hook
        BindElementEvents(e.Element);

        if (e.Element is VisualElement visualElement)
        {
            BindVisualElementEvents(visualElement);
        }

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
    }

    private void OnApplicationOnDescendantRemoved(object? _, ElementEventArgs e)
    {
        // All elements have a set of common events we can hook
        UnbindElementEvents(e.Element);

        if (e.Element is VisualElement visualElement)
        {
            UnbindVisualElementEvents(visualElement);
        }

        // We can also attach to specific events on built-in controls
        // Be sure to update ExplicitlyHandledTypes when adding to this list
        switch (e.Element)
        {
            case Window window:
                UnbindWindowEvents(window);
                break;
            case Shell shell:
                UnbindShellEvents(shell);
                break;
            case Page page:
                UnbindPageEvents(page);
                break;
            case Button button:
                UnbindButtonEvents(button);
                break;
        }
    }

    private void BindWindowEvents(Window window)
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

    private void UnbindWindowEvents(Window window)
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

    private void BindElementEvents(Element element)
    {
        if (_options.CreateElementEventBreadcrumbs)
        {
            return;
        }

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

    private void UnbindElementEvents(Element element)
    {
        if (_options.CreateElementEventBreadcrumbs)
        {
            return;
        }

        // Rendering events
        element.ChildAdded -= MauiEvents.OnElementOnChildAdded;
        element.ChildRemoved -= MauiEvents.OnElementOnChildRemoved;
        element.ParentChanged -= MauiEvents.OnElementOnParentChanged;

        // BindableObject events
        element.BindingContextChanged -= MauiEvents.OnElementOnBindingContextChanged;
    }

    private void BindVisualElementEvents(VisualElement element)
    {
        element.Focused += MauiEvents.OnElementOnFocused;
        element.Unfocused += MauiEvents.OnElementOnUnfocused;
    }

    private void UnbindVisualElementEvents(VisualElement element)
    {
        element.Focused -= MauiEvents.OnElementOnFocused;
        element.Unfocused -= MauiEvents.OnElementOnUnfocused;
    }

    private void BindShellEvents(Shell shell)
    {
        // Navigation events
        // https://docs.microsoft.com/dotnet/maui/fundamentals/shell/navigation
        shell.Navigating += MauiEvents.OnShellOnNavigating;
        shell.Navigated += MauiEvents.OnShellOnNavigated;

        // A Shell is also a Page
        BindPageEvents(shell);
    }

    private void UnbindShellEvents(Shell shell)
    {
        // Navigation events
        // https://docs.microsoft.com/dotnet/maui/fundamentals/shell/navigation
        shell.Navigating -= MauiEvents.OnShellOnNavigating;
        shell.Navigated -= MauiEvents.OnShellOnNavigated;

        // A Shell is also a Page
        UnbindPageEvents(shell);
    }

    private void BindPageEvents(Page page)
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

    private void UnbindPageEvents(Page page)
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


    private void BindButtonEvents(Button button)
    {
        button.Clicked += MauiEvents.OnButtonOnClicked;
        button.Pressed += MauiEvents.OnButtonOnPressed;
        button.Released += MauiEvents.OnButtonOnReleased;
    }

    private void UnbindButtonEvents(Button button)
    {
        button.Clicked -= MauiEvents.OnButtonOnClicked;
        button.Pressed -= MauiEvents.OnButtonOnPressed;
        button.Released -= MauiEvents.OnButtonOnReleased;
    }
}
