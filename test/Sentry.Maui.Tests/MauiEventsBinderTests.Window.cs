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
        _fixture.Binder.BindWindowEvents(window);

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
    [InlineData(true)]
    [InlineData(false)]
    public void Window_Backgrounding_AddsBreadcrumb(bool includeStateInBreadcrumb)
    {
        // Arrange
        var window = new Window
        {
            StyleId = "window"
        };
        _fixture.Binder.BindWindowEvents(window);
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
    public void Window_DisplayDensityChanged_AddsBreadcrumb()
    {
        // Arrange
        var window = new Window
        {
            StyleId = "window"
        };
        _fixture.Binder.BindWindowEvents(window);

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
    public void Window_PopCanceled_AddsBreadcrumb()
    {
        // Arrange
        var window = new Window
        {
            StyleId = "window"
        };
        _fixture.Binder.BindWindowEvents(window);

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

    [Theory]
    [MemberData(nameof(WindowModalEventsData))]
    public void Window_ModalEvents_AddsBreadcrumb(string eventName, object eventArgs)
    {
        // Arrange
        var window = new Window
        {
            StyleId = "window"
        };
        _fixture.Binder.BindWindowEvents(window);

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
                new object[] {nameof(Window.ModalPushing), new ModalPushingEventArgs(modelPage)},
                new object[] {nameof(Window.ModalPushed), new ModalPushedEventArgs(modelPage)},
                new object[] {nameof(Window.ModalPopping), new ModalPoppingEventArgs(modelPage)},
                new object[] {nameof(Window.ModalPopped), new ModalPoppedEventArgs(modelPage)}
            };
        }
    }
}
