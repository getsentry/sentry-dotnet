using Microsoft.Extensions.Options;
using Sentry.Internal;

namespace Sentry.Maui.Internal;

internal interface IMauiEventsBinder
{
    public void HandleApplicationEvents(Application application, bool bind = true);
}

internal class MauiEventsBinder : IMauiEventsBinder
{
    private readonly IHub _hub;
    private readonly SentryMauiOptions _options;
    internal readonly IEnumerable<IMauiElementEventBinder> _elementEventBinders;

    // Tracks the active auto-finishing navigation transaction so we can explicitly finish it early
    // (e.g. when the next navigation begins) before the idle timeout would fire.
    private ITransactionTracer? _currentTransaction;

    // Tracks the active auto-finishing user-interaction (click) transaction, separate from navigation
    // so that navigation can become a child span of the click transaction.
    private ITransactionTracer? _currentInteractionTransaction;

    // Tracks the navigation child span when navigation is nested under a click transaction.
    private ISpan? _currentNavigationSpan;

    // https://develop.sentry.dev/sdk/event-payloads/breadcrumbs/#breadcrumb-types
    // https://github.com/getsentry/sentry/blob/master/static/app/types/breadcrumbs.tsx
    internal const string NavigationType = "navigation";
    internal const string SystemType = "system";
    internal const string UserType = "user";
    internal const string LifecycleCategory = "ui.lifecycle";
    internal const string NavigationCategory = "navigation";
    internal const string RenderingCategory = "ui.rendering";
    internal const string UserActionCategory = "ui.useraction";
    internal const string UserInteractionClickOp = "ui.action.click";

    public MauiEventsBinder(IHub hub, IOptions<SentryMauiOptions> options, IEnumerable<IMauiElementEventBinder> elementEventBinders)
    {
        _hub = hub;
        _options = options.Value;
        _elementEventBinders = elementEventBinders.Where(b
            => b is not MauiSessionReplayMaskControlsOfTypeBinder maskControlTypeBinder
               || maskControlTypeBinder.IsEnabled);
    }

    public void HandleApplicationEvents(Application application, bool bind = true)
    {
        // we always unbind first to ensure no previous hooks
        UnbindApplication(application);

        if (bind)
        {
            // Attach element events to all existing descendants (skip the application itself)
            foreach (var descendant in application.GetVisualTreeDescendants().Skip(1))
            {
                if (descendant is VisualElement element)
                {
                    OnApplicationOnDescendantAdded(application, new ElementEventArgs(element));
                }
            }

            // Attach element events to all descendants as they are added to the application.
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
            application.ModalPushing += OnApplicationOnModalPushing;
            application.ModalPushed += OnApplicationOnModalPushed;
            application.ModalPopping += OnApplicationOnModalPopping;
            application.ModalPopped += OnApplicationOnModalPopped;

            // Theme changed event
            // https://docs.microsoft.com/dotnet/maui/user-interface/system-theme-changes#react-to-theme-changes
            application.RequestedThemeChanged += OnApplicationOnRequestedThemeChanged;
        }
    }

    private void UnbindApplication(Application application)
    {
        application.DescendantAdded -= OnApplicationOnDescendantAdded;
        application.DescendantRemoved -= OnApplicationOnDescendantRemoved;

        HandleElementEvents(application, bind: false);

        // Navigation events
        application.PageAppearing -= OnApplicationOnPageAppearing;
        application.PageDisappearing -= OnApplicationOnPageDisappearing;
        application.ModalPushing -= OnApplicationOnModalPushing;
        application.ModalPushed -= OnApplicationOnModalPushed;
        application.ModalPopping -= OnApplicationOnModalPopping;
        application.ModalPopped -= OnApplicationOnModalPopped;

        // Theme changed event
        application.RequestedThemeChanged -= OnApplicationOnRequestedThemeChanged;
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

        if (_options.EnableAutoTransactions
            && _options.EnableUserInteractionTracing)
        {
            switch (e.Element)
            {
                case Button button:
                    button.Pressed += OnElementPressedForTransaction;
                    break;
                case ImageButton imageButton:
                    imageButton.Pressed += OnElementPressedForTransaction;
                    break;
            }
        }
    }

    internal void OnBreadcrumbCreateCallback(BreadcrumbEvent breadcrumb)
    {
        _hub.AddBreadcrumbForEvent(
            _options,
            breadcrumb.Sender,
            breadcrumb.EventName,
            UserType,
            UserActionCategory,
            extra =>
            {
                foreach (var (key, value) in breadcrumb.ExtraData)
                {
                    extra[key] = value;
                }
            }
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

        switch (e.Element)
        {
            case Button button:
                button.Pressed -= OnElementPressedForTransaction;
                break;
            case ImageButton imageButton:
                imageButton.Pressed -= OnElementPressedForTransaction;
                break;
        }
    }

    internal void HandleWindowEvents(Window window, bool bind = true)
    {
        UnhookWindow(window);
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
    }

    private void UnhookWindow(Window window)
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

    internal void HandleElementEvents(Element element, bool bind = true)
    {
        // we always unbind the element first to ensure we don't have any sticky or repeat hooks
        // Rendering events
        element.ChildAdded -= OnElementOnChildAdded;
        element.ChildRemoved -= OnElementOnChildRemoved;
        element.ParentChanged -= OnElementOnParentChanged;

        // BindableObject events
        element.BindingContextChanged -= OnElementOnBindingContextChanged;

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
    }

    internal void HandleVisualElementEvents(VisualElement element, bool bind = true)
    {
        element.Focused -= OnElementOnFocused;
        element.Unfocused -= OnElementOnUnfocused;

        if (bind)
        {
            element.Focused += OnElementOnFocused;
            element.Unfocused += OnElementOnUnfocused;
        }
    }

    internal void HandleShellEvents(Shell shell, bool bind = true)
    {
        // Navigation events
        // https://docs.microsoft.com/dotnet/maui/fundamentals/shell/navigation
        shell.Navigating -= OnShellOnNavigating;
        shell.Navigated -= OnShellOnNavigated;

        // A Shell is also a Page
        HandlePageEvents(shell, bind: false);

        if (bind)
        {
            // Navigation events
            // https://docs.microsoft.com/dotnet/maui/fundamentals/shell/navigation
            shell.Navigating += OnShellOnNavigating;
            shell.Navigated += OnShellOnNavigated;

            // A Shell is also a Page
            HandlePageEvents(shell);
        }
    }

    internal void HandlePageEvents(Page page, bool bind = true)
    {
        // Lifecycle events
        // https://docs.microsoft.com/dotnet/maui/fundamentals/shell/lifecycle
        page.Appearing -= OnPageOnAppearing;
        page.Disappearing -= OnPageOnDisappearing;

        // Navigation events
        // https://github.com/dotnet/docs-maui/issues/583
        page.NavigatedTo -= OnPageOnNavigatedTo;

        // Size changed event
        // https://learn.microsoft.com/dotnet/api/microsoft.maui.controls.visualelement.sizechanged
        page.SizeChanged -= OnPageOnSizeChanged;

        if (bind)
        {
            // Lifecycle events
            // https://docs.microsoft.com/dotnet/maui/fundamentals/shell/lifecycle
            page.Appearing += OnPageOnAppearing;
            page.Disappearing += OnPageOnDisappearing;

            // Navigation events
            // https://github.com/dotnet/docs-maui/issues/583
            page.NavigatedTo += OnPageOnNavigatedTo;

            // Size changed event
            // https://learn.microsoft.com/dotnet/api/microsoft.maui.controls.visualelement.sizechanged
            page.SizeChanged += OnPageOnSizeChanged;
        }
    }

    private ITransactionTracer? StartNavigationTransaction(string name)
    {
        // When a click transaction is active, navigation becomes a child span of it.
        if (_currentInteractionTransaction is { IsFinished: false } clickTx)
        {
            // Only explicitly finish a previous standalone nav tx if it has child spans.
            if (_currentTransaction is { IsFinished: false } prevNavTx && prevNavTx.Spans.Count > 0)
            {
                prevNavTx.Finish(SpanStatus.Ok);
            }
            _currentTransaction = null;

            _currentNavigationSpan?.Finish(SpanStatus.Ok);
            _currentNavigationSpan = clickTx.StartChild("ui.load", name);
            return clickTx;
        }

        // Standalone navigation — no active click transaction.
        _currentNavigationSpan = null;

        if (_currentTransaction is { IsFinished: false } previousTx)
        {
            if (previousTx.Name == name)
            {
                // Same destination — reset idle timeout instead of starting a new transaction.
                previousTx.ResetIdleTimeout();
                return previousTx;
            }

            // Different destination — only finish if it has child spans.
            // Childless transactions will be discarded by the idle timeout.
            if (previousTx.Spans.Count > 0)
            {
                previousTx.Finish(SpanStatus.Ok);
            }
        }

        var context = new TransactionContext(name, "ui.load")
        {
            NameSource = TransactionNameSource.Route
        };

        var transaction = _hub is IHubInternal internalHub
            ? internalHub.StartTransaction(context, _options.AutoTransactionIdleTimeout)
            : _hub.StartTransaction(context);

        // Only bind to scope if there is no user-created transaction already there.
        var hasUserTransaction = false;
        _hub.ConfigureScope(scope =>
        {
            if (scope.Transaction is { } existing && !ReferenceEquals(existing, _currentTransaction))
            {
                hasUserTransaction = true;
            }
        });
        if (!hasUserTransaction)
        {
            _hub.ConfigureScope(static (scope, t) => scope.Transaction = t, transaction);
        }

        _currentTransaction = transaction;
        return transaction;
    }

    private void OnElementPressedForTransaction(object? sender, EventArgs _)
    {
        if (sender is not Element element)
        {
            return;
        }

        string? identifier = null;
        if (!string.IsNullOrEmpty(element.AutomationId))
        {
            identifier = element.AutomationId;
        }
        else if (!string.IsNullOrEmpty(element.StyleId))
        {
            identifier = element.StyleId;
        }

        if (identifier is null)
        {
            _options.DiagnosticLogger?.Log(
                SentryLevel.Warning,
                "Click transaction skipped: element has no AutomationId or StyleId");
            return;
        }

        var pageName = element.FindContainingPage()?.GetType().Name;
        var name = pageName != null ? $"{pageName}.{identifier}" : identifier;
        StartUserInteractionTransaction(name);
    }

    private ITransactionTracer? StartUserInteractionTransaction(string name)
    {
        // Each click always creates a new transaction.
        // Finish any previous SDK-owned interaction transaction (and its navigation child span).
        if (_currentNavigationSpan is { IsFinished: false })
        {
            _currentNavigationSpan.Finish(SpanStatus.Cancelled);
            _currentNavigationSpan = null;
        }
        // Only explicitly finish the previous click transaction if it has child spans.
        // Childless transactions will be discarded by the idle timeout.
        if (_currentInteractionTransaction is { IsFinished: false } prevTx && prevTx.Spans.Count > 0)
        {
            prevTx.Finish(SpanStatus.Ok);
        }

        var context = new TransactionContext(name, UserInteractionClickOp)
        {
            NameSource = TransactionNameSource.Component
        };

        var transaction = _hub is IHubInternal internalHub
            ? internalHub.StartTransaction(context, _options.AutoTransactionIdleTimeout)
            : _hub.StartTransaction(context);

        // Only bind to scope if there is no other transaction already there (user-created or SDK-owned navigation).
        var hasOtherTransaction = false;
        _hub.ConfigureScope(scope =>
        {
            if (scope.Transaction is { } existing && !ReferenceEquals(existing, _currentInteractionTransaction))
            {
                hasOtherTransaction = true;
            }
        });
        if (!hasOtherTransaction)
        {
            _hub.ConfigureScope(static (scope, t) => scope.Transaction = t, transaction);
        }

        _currentInteractionTransaction = transaction;
        return transaction;
    }

    private void FinishNavigationSpanOrTransaction()
    {
        if (_currentNavigationSpan is { IsFinished: false } navSpan)
        {
            navSpan.Finish(SpanStatus.Ok);
            _currentNavigationSpan = null;
        }
        // For standalone navigation transactions, don't explicitly finish.
        // The idle timeout will capture them if they have child spans,
        // or discard them if they don't.
    }

    // Application Events

    private void OnApplicationOnPageAppearing(object? sender, Page page) =>
        _hub.AddBreadcrumbForEvent(_options, sender, nameof(Application.PageAppearing), NavigationType, NavigationCategory, data => data.AddElementInfo(_options, page, nameof(Page)));
    private void OnApplicationOnPageDisappearing(object? sender, Page page) =>
        _hub.AddBreadcrumbForEvent(_options, sender, nameof(Application.PageDisappearing), NavigationType, NavigationCategory, data => data.AddElementInfo(_options, page, nameof(Page)));

    private void OnApplicationOnModalPushing(object? sender, ModalPushingEventArgs e) =>
        _hub.AddBreadcrumbForEvent(_options, sender, nameof(Application.ModalPushing), NavigationType, NavigationCategory, data => data.AddElementInfo(_options, e.Modal, nameof(e.Modal)));

    private void OnApplicationOnModalPushed(object? sender, ModalPushedEventArgs e) =>
        _hub.AddBreadcrumbForEvent(_options, sender, nameof(Application.ModalPushed), NavigationType, NavigationCategory, data => data.AddElementInfo(_options, e.Modal, nameof(e.Modal)));

    private void OnApplicationOnModalPopping(object? sender, ModalPoppingEventArgs e) =>
        _hub.AddBreadcrumbForEvent(_options, sender, nameof(Application.ModalPopping), NavigationType, NavigationCategory, data => data.AddElementInfo(_options, e.Modal, nameof(e.Modal)));

    private void OnApplicationOnModalPopped(object? sender, ModalPoppedEventArgs e) =>
        _hub.AddBreadcrumbForEvent(_options, sender, nameof(Application.ModalPopped), NavigationType, NavigationCategory, data => data.AddElementInfo(_options, e.Modal, nameof(e.Modal)));
    private void OnApplicationOnRequestedThemeChanged(object? sender, AppThemeChangedEventArgs e) =>
        _hub.AddBreadcrumbForEvent(_options, sender, nameof(Application.RequestedThemeChanged), SystemType, RenderingCategory, data => data.Add(nameof(e.RequestedTheme), e.RequestedTheme.ToString()));

    // Window Events

    private void OnWindowOnActivated(object? sender, EventArgs _) =>
        _hub.AddBreadcrumbForEvent(_options, sender, nameof(Window.Activated), SystemType, LifecycleCategory);

    private void OnWindowOnDeactivated(object? sender, EventArgs _) =>
        _hub.AddBreadcrumbForEvent(_options, sender, nameof(Window.Deactivated), SystemType, LifecycleCategory);

    private void OnWindowOnStopped(object? sender, EventArgs _)
    {
        _hub.AddBreadcrumbForEvent(_options, sender, nameof(Window.Stopped), SystemType, LifecycleCategory);
        if (_options.EnableAutoTransactions)
        {
            _currentNavigationSpan?.Finish(SpanStatus.Ok);
            _currentNavigationSpan = null;
            _currentTransaction?.Finish(SpanStatus.Ok);
            _currentTransaction = null;
            _currentInteractionTransaction?.Finish(SpanStatus.Ok);
            _currentInteractionTransaction = null;
        }
    }

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

    private void OnWindowOnPopCanceled(object? sender, EventArgs _)
    {
        _hub.AddBreadcrumbForEvent(_options, sender, nameof(Window.PopCanceled), NavigationType, NavigationCategory);
        if (_options.EnableAutoTransactions && _currentNavigationSpan is { IsFinished: false } navSpan)
        {
            navSpan.Finish(SpanStatus.Cancelled);
            _currentNavigationSpan = null;
        }
    }

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

    private void OnShellOnNavigating(object? sender, ShellNavigatingEventArgs e)
    {
        _hub.AddBreadcrumbForEvent(_options, sender, nameof(Shell.Navigating), NavigationType, NavigationCategory, data =>
        {
            data.Add("from", e.Current?.Location.ToString() ?? "");
            data.Add("to", e.Target?.Location.ToString() ?? "");
            data.Add(nameof(e.Source), e.Source.ToString());
        });

        if (_options.EnableAutoTransactions)
        {
            StartNavigationTransaction(e.Target?.Location.ToString() ?? "Unknown");
        }
    }

    private void OnShellOnNavigated(object? sender, ShellNavigatedEventArgs e)
    {
        _hub.AddBreadcrumbForEvent(_options, sender, nameof(Shell.Navigated), NavigationType, NavigationCategory, data =>
        {
            data.Add("from", e.Previous?.Location.ToString() ?? "");
            data.Add("to", e.Current?.Location.ToString() ?? "");
            data.Add(nameof(e.Source), e.Source.ToString());
        });

        // Update to the final resolved route now that navigation is confirmed, then finish
        if (_options.EnableAutoTransactions)
        {
            var resolvedRoute = e.Current?.Location.ToString();
            if (resolvedRoute == null)
            {
                return;
            }

            if (_currentNavigationSpan is { IsFinished: false } navSpan)
            {
                navSpan.Description = resolvedRoute;
            }
            else if (_currentTransaction != null)
            {
                _currentTransaction.Name = resolvedRoute;
            }

            FinishNavigationSpanOrTransaction();
        }
    }

    // Page Events

    private void OnPageOnAppearing(object? sender, EventArgs _) =>
        _hub.AddBreadcrumbForEvent(_options, sender, nameof(Page.Appearing), SystemType, LifecycleCategory);

    private void OnPageOnDisappearing(object? sender, EventArgs _) =>
        _hub.AddBreadcrumbForEvent(_options, sender, nameof(Page.Disappearing), SystemType, LifecycleCategory);

    private void OnPageOnNavigatedTo(object? sender, NavigatedToEventArgs e) =>
        _hub.AddBreadcrumbForEvent(_options, sender, nameof(Page.NavigatedTo), NavigationType, NavigationCategory, data => data.AddElementInfo(_options, e.GetPreviousPage(), "PreviousPage"));

    private void OnPageOnSizeChanged(object? sender, EventArgs _) =>
        _hub.AddBreadcrumbForEvent(_options, sender, nameof(Page.SizeChanged), SystemType, RenderingCategory);
}
