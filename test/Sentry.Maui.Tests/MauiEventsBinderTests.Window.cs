using Sentry.Maui.Internal;

namespace Sentry.Maui.Tests;

public partial class MauiEventsBinderTests
{
    [Theory]
    [InlineData(nameof(Window.Activated))]
    [InlineData(nameof(Window.Deactivated))]
    [InlineData(nameof(Window.Stopped))]
    [InlineData(nameof(Window.Resumed))]
    [InlineData(nameof(Window.Created))]
    [InlineData(nameof(Window.Destroying))]
    public void Window_LifecycleEvents_AddsBreadcrumb(string eventName)
    {
        // Arrange
        var window = new Window
        {
            StyleId = "window"
        };
        _fixture.Binder.HandleWindowEvents(window);

        // Act
        window.RaiseEvent(eventName, EventArgs.Empty);

        // Assert
        var crumb = Assert.Single(_fixture.Scope.Breadcrumbs);
        Assert.Equal($"{nameof(Window)}.{eventName}", crumb.Message);
        Assert.Equal(BreadcrumbLevel.Info, crumb.Level);
        Assert.Equal(MauiEventsBinder.SystemType, crumb.Type);
        Assert.Equal(MauiEventsBinder.LifecycleCategory, crumb.Category);
        crumb.Data.Should().Contain($"{nameof(Window)}.Name", "window");
    }

    [Theory]
    [InlineData(nameof(Window.Activated))]
    [InlineData(nameof(Window.Deactivated))]
    [InlineData(nameof(Window.Stopped))]
    [InlineData(nameof(Window.Resumed))]
    [InlineData(nameof(Window.Created))]
    [InlineData(nameof(Window.Destroying))]
    public void Window_UnbindLifecycleEvents_DoesNotAddBreadcrumb(string eventName)
    {
        // Arrange
        var window = new Window
        {
            StyleId = "window"
        };
        _fixture.Binder.HandleWindowEvents(window);

        window.RaiseEvent(eventName, EventArgs.Empty);
        Assert.Equal(1, _fixture.Scope.Breadcrumbs.Count); // Sanity check

        _fixture.Binder.HandleWindowEvents(window, bind: false);

        // Act
        window.RaiseEvent(eventName, EventArgs.Empty);

        // Assert
        Assert.Equal(1, _fixture.Scope.Breadcrumbs.Count);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Window_Backgrounding_AddsBreadcrumb(bool includeStateInBreadcrumb)
    {
        // Arrange
        var window = new Window
        {
            StyleId = "window"
        };
        _fixture.Binder.HandleWindowEvents(window);
        _fixture.Options.IncludeBackgroundingStateInBreadcrumbs = includeStateInBreadcrumb;

        var state = new PersistedState
        {
            ["Foo"] = "123",
            ["Bar"] = "",
            ["Baz"] = null
        };

        // Act
        window.RaiseEvent(nameof(Window.Backgrounding), new BackgroundingEventArgs(state));

        // Assert
        var crumb = Assert.Single(_fixture.Scope.Breadcrumbs);
        Assert.Equal($"{nameof(Window)}.{nameof(Window.Backgrounding)}", crumb.Message);
        Assert.Equal(BreadcrumbLevel.Info, crumb.Level);
        Assert.Equal(MauiEventsBinder.SystemType, crumb.Type);
        Assert.Equal(MauiEventsBinder.LifecycleCategory, crumb.Category);
        crumb.Data.Should().Contain($"{nameof(Window)}.Name", "window");

        if (includeStateInBreadcrumb)
        {
            crumb.Data.Should().Contain("State.Foo", "123");
            crumb.Data.Should().Contain("State.Bar", "");
            crumb.Data.Should().Contain("State.Baz", "<null>");
        }
        else
        {
            crumb.Data.Should().NotContain(kvp => kvp.Key.StartsWith("State."));
        }
    }

    [Fact]
    public void Window_UnbindBackgrounding_DoesNotAddBreadcrumb()
    {
        // Arrange
        var window = new Window
        {
            StyleId = "window"
        };
        _fixture.Binder.HandleWindowEvents(window);
        _fixture.Options.IncludeBackgroundingStateInBreadcrumbs = true;

        var state = new PersistedState
        {
            ["Foo"] = "123",
            ["Bar"] = "",
            ["Baz"] = null
        };

        window.RaiseEvent(nameof(Window.Backgrounding), new BackgroundingEventArgs(state));
        Assert.Equal(1, _fixture.Scope.Breadcrumbs.Count); // Sanity check

        _fixture.Binder.HandleWindowEvents(window, bind: false);

        // Act
        window.RaiseEvent(nameof(Window.Backgrounding), new BackgroundingEventArgs(state));

        // Assert
        Assert.Equal(1, _fixture.Scope.Breadcrumbs.Count);
    }

    [Fact]
    public void Window_DisplayDensityChanged_AddsBreadcrumb()
    {
        // Arrange
        var window = new Window
        {
            StyleId = "window"
        };
        _fixture.Binder.HandleWindowEvents(window);

        // Act
        window.RaiseEvent(nameof(Window.DisplayDensityChanged), new DisplayDensityChangedEventArgs(1.25f));

        // Assert
        var crumb = Assert.Single(_fixture.Scope.Breadcrumbs);
        Assert.Equal($"{nameof(Window)}.{nameof(Window.DisplayDensityChanged)}", crumb.Message);
        Assert.Equal(BreadcrumbLevel.Info, crumb.Level);
        Assert.Equal(MauiEventsBinder.SystemType, crumb.Type);
        Assert.Equal(MauiEventsBinder.LifecycleCategory, crumb.Category);
        crumb.Data.Should().Contain($"{nameof(Window)}.Name", "window");
        crumb.Data.Should().Contain("DisplayDensity", "1.25");
    }

    [Fact]
    public void Window_UnbindDisplayDensityChanged_DoesNotAddBreadcrumb()
    {
        // Arrange
        var window = new Window
        {
            StyleId = "window"
        };
        _fixture.Binder.HandleWindowEvents(window);

        window.RaiseEvent(nameof(Window.DisplayDensityChanged), new DisplayDensityChangedEventArgs(1.25f));
        Assert.Equal(1, _fixture.Scope.Breadcrumbs.Count); // Sanity check

        _fixture.Binder.HandleWindowEvents(window, bind: false);

        // Act
        window.RaiseEvent(nameof(Window.DisplayDensityChanged), new DisplayDensityChangedEventArgs(1.0f));

        // Assert
        Assert.Equal(1, _fixture.Scope.Breadcrumbs.Count);
    }

    [Fact]
    public void Window_PopCanceled_AddsBreadcrumb()
    {
        // Arrange
        var window = new Window
        {
            StyleId = "window"
        };
        _fixture.Binder.HandleWindowEvents(window);

        // Act
        window.RaiseEvent(nameof(Window.PopCanceled), EventArgs.Empty);

        // Assert
        var crumb = Assert.Single(_fixture.Scope.Breadcrumbs);
        Assert.Equal($"{nameof(Window)}.{nameof(Window.PopCanceled)}", crumb.Message);
        Assert.Equal(BreadcrumbLevel.Info, crumb.Level);
        Assert.Equal(MauiEventsBinder.NavigationType, crumb.Type);
        Assert.Equal(MauiEventsBinder.NavigationCategory, crumb.Category);
        crumb.Data.Should().Contain($"{nameof(Window)}.Name", "window");
    }

    [Fact]
    public void Window_UnbindPopCanceled_DoesNotAddBreadcrumb()
    {
        // Arrange
        var window = new Window
        {
            StyleId = "window"
        };
        _fixture.Binder.HandleWindowEvents(window);

        window.RaiseEvent(nameof(Window.PopCanceled), EventArgs.Empty);
        Assert.Equal(1, _fixture.Scope.Breadcrumbs.Count); // Sanity check

        _fixture.Binder.HandleWindowEvents(window, bind: false);

        // Act
        window.RaiseEvent(nameof(Window.PopCanceled), EventArgs.Empty);

        // Assert
        Assert.Equal(1, _fixture.Scope.Breadcrumbs.Count);
    }

    [Theory]
    [MemberData(nameof(WindowModalEventsData))]
    public void Window_ModalEvents_AddsBreadcrumb(string eventName, object eventArgs)
    {
        // Arrange
        var window = new Window
        {
            StyleId = "window"
        };
        _fixture.Binder.HandleWindowEvents(window);

        // Act
        window.RaiseEvent(eventName, eventArgs);

        // Assert
        var crumb = Assert.Single(_fixture.Scope.Breadcrumbs);
        Assert.Equal($"{nameof(Window)}.{eventName}", crumb.Message);
        Assert.Equal(BreadcrumbLevel.Info, crumb.Level);
        Assert.Equal(MauiEventsBinder.NavigationType, crumb.Type);
        Assert.Equal(MauiEventsBinder.NavigationCategory, crumb.Category);
        crumb.Data.Should().Contain($"{nameof(Window)}.Name", "window");
        crumb.Data.Should().Contain("Modal", nameof(ContentPage));
        crumb.Data.Should().Contain("Modal.Name", "TestModalPage");
    }

    [Theory]
    [MemberData(nameof(WindowModalEventsData))]
    public void Window_UnbindModalEvents_DoesNotAddBreadcrumb(string eventName, object eventArgs)
    {
        // Arrange
        var window = new Window
        {
            StyleId = "window"
        };
        _fixture.Binder.HandleWindowEvents(window);

        window.RaiseEvent(eventName, eventArgs);
        Assert.Equal(1, _fixture.Scope.Breadcrumbs.Count); // Sanity check

        _fixture.Binder.HandleWindowEvents(window, bind: false);

        // Act
        window.RaiseEvent(eventName, eventArgs);

        // Assert
        Assert.Equal(1, _fixture.Scope.Breadcrumbs.Count);
    }

    public static IEnumerable<object[]> WindowModalEventsData
    {
        get
        {
            var modelPage = new ContentPage
            {
                StyleId = "TestModalPage"
            };

            return new List<object[]>
            {
                // Note, these are distinct from the Application events with the same names.
                new object[] {nameof(Window.ModalPushed), new ModalPushedEventArgs(modelPage)},
                new object[] {nameof(Window.ModalPopped), new ModalPoppedEventArgs(modelPage)}
            };
        }
    }
}
