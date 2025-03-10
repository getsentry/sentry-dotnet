using Sentry.Maui.Internal;
using Sentry.Maui.Tests.Mocks;

namespace Sentry.Maui.Tests;

public partial class MauiEventsBinderTests
{
    [Theory]
    [InlineData(nameof(Application.ChildAdded))]
    [InlineData(nameof(Application.ChildRemoved))]
    public void Application_ChildElementEvents_AddsBreadcrumb(string eventName)
    {
        // Arrange
        _fixture.Options.CreateElementEventsBreadcrumbs = true;
        var application = MockApplication.Create();
        _fixture.Binder.HandleApplicationEvents(application);

        var element = Substitute.For<Element>();

        // Act
        application.RaiseEvent(eventName, new ElementEventArgs(element));

        // Assert
        var crumb = Assert.Single(_fixture.Scope.Breadcrumbs);
        Assert.Equal($"{nameof(MockApplication)}.{eventName}", crumb.Message);
        Assert.Equal(BreadcrumbLevel.Info, crumb.Level);
        Assert.Equal(MauiEventsBinder.SystemType, crumb.Type);
        Assert.Equal(MauiEventsBinder.RenderingCategory, crumb.Category);
        crumb.Data.Should().Contain("Element", element.ToString());
    }

    [Theory]
    [InlineData(nameof(Application.ChildAdded))]
    [InlineData(nameof(Application.ChildRemoved))]
    public void Application_UnbindChildElementEvents_DoesNotAddBreadcrumb(string eventName)
    {
        // Arrange
        _fixture.Options.CreateElementEventsBreadcrumbs = true;
        var application = MockApplication.Create();
        _fixture.Binder.HandleApplicationEvents(application);

        var element = Substitute.For<Element>();
        application.RaiseEvent(eventName, new ElementEventArgs(element));
        Assert.Single(_fixture.Scope.Breadcrumbs); // Sanity check

        _fixture.Binder.HandleApplicationEvents(application, bind: false);

        // Act
        application.RaiseEvent(eventName, new ElementEventArgs(element));

        // Assert
        application.RaiseEvent(eventName, new ElementEventArgs(element));
        Assert.Single(_fixture.Scope.Breadcrumbs);
    }

    [Theory]
    [InlineData(nameof(Application.PageAppearing))]
    [InlineData(nameof(Application.PageDisappearing))]
    public void Application_PageEvents_AddsBreadcrumb(string eventName)
    {
        // Arrange
        var application = MockApplication.Create();
        _fixture.Binder.HandleApplicationEvents(application);
        var page = new ContentPage
        {
            StyleId = "TestPage"
        };

        // Act
        application.RaiseEvent(eventName, page);

        // Assert
        var crumb = Assert.Single(_fixture.Scope.Breadcrumbs);
        Assert.Equal($"{nameof(MockApplication)}.{eventName}", crumb.Message);
        Assert.Equal(BreadcrumbLevel.Info, crumb.Level);
        Assert.Equal(MauiEventsBinder.NavigationType, crumb.Type);
        Assert.Equal(MauiEventsBinder.NavigationCategory, crumb.Category);
        crumb.Data.Should().Contain("Page", nameof(ContentPage));
        crumb.Data.Should().Contain("Page.Name", page.StyleId);
    }

    [Theory]
    [InlineData(nameof(Application.PageAppearing))]
    [InlineData(nameof(Application.PageDisappearing))]
    public void Application_UnbindPageEvents_DoesNotAddBreadcrumb(string eventName)
    {
        // Arrange
        var application = MockApplication.Create();
        _fixture.Binder.HandleApplicationEvents(application);
        var page = new ContentPage
        {
            StyleId = "TestPage"
        };

        application.RaiseEvent(eventName, page);
        Assert.Single(_fixture.Scope.Breadcrumbs); // Sanity check

        _fixture.Binder.HandleApplicationEvents(application, bind: false);

        // Act
        application.RaiseEvent(eventName, page);

        // Assert
        Assert.Single(_fixture.Scope.Breadcrumbs);
    }

    [Theory]
    [MemberData(nameof(ApplicationModalEventsData))]
    public void Application_ModalEvents_AddsBreadcrumb(string eventName, object eventArgs)
    {
        // Arrange
        var application = MockApplication.Create();
        _fixture.Binder.HandleApplicationEvents(application);

        // Act
        application.RaiseEvent(eventName, eventArgs);

        // Assert
        var crumb = Assert.Single(_fixture.Scope.Breadcrumbs);
        Assert.Equal($"{nameof(MockApplication)}.{eventName}", crumb.Message);
        Assert.Equal(BreadcrumbLevel.Info, crumb.Level);
        Assert.Equal(MauiEventsBinder.NavigationType, crumb.Type);
        Assert.Equal(MauiEventsBinder.NavigationCategory, crumb.Category);
        crumb.Data.Should().Contain("Modal", nameof(ContentPage));
        crumb.Data.Should().Contain("Modal.Name", "TestModalPage");
    }

    [Theory]
    [MemberData(nameof(ApplicationModalEventsData))]
    public void Application_UnbindModalEvents_DoesNotAddBreadcrumb(string eventName, object eventArgs)
    {
        // Arrange
        var application = MockApplication.Create();
        _fixture.Binder.HandleApplicationEvents(application);

        application.RaiseEvent(eventName, eventArgs);
        Assert.Single(_fixture.Scope.Breadcrumbs); // Sanity check

        _fixture.Binder.HandleApplicationEvents(application, bind: false);

        // Act
        application.RaiseEvent(eventName, eventArgs);

        // Assert
        Assert.Single(_fixture.Scope.Breadcrumbs);
    }

    [Fact]
    public void Application_HandleApplicationEvents_TracksExistingDescendants()
    {
        // Arrange
        var application = MockApplication.Create();
        var element = new MockVisualElement("element");
        var mainPage = new ContentPage
        {
            Content = new VerticalStackLayout
            {
                element
            }
        };

        application.AddWindow(mainPage);

        _fixture.Binder.HandleApplicationEvents(application);

        // Act
        element.RaiseEvent(nameof(VisualElement.Focused), new FocusEventArgs(element, true));

        // Assert
        var crumb = Assert.Single(_fixture.Scope.Breadcrumbs);
        Assert.Equal($"{nameof(MockVisualElement)}.{nameof(VisualElement.Focused)}", crumb.Message);
        Assert.Equal(BreadcrumbLevel.Info, crumb.Level);
        Assert.Equal(MauiEventsBinder.SystemType, crumb.Type);
        Assert.Equal(MauiEventsBinder.RenderingCategory, crumb.Category);
        crumb.Data.Should().Contain($"{nameof(MockVisualElement)}.Name", "element");
    }

    public static IEnumerable<object[]> ApplicationModalEventsData
    {
        get
        {
            var modelPage = new ContentPage
            {
                StyleId = "TestModalPage"
            };

            return new List<object[]>
            {
                // Note, these are distinct from the Window events with the same names.
                new object[] {nameof(Application.ModalPushed), new ModalPushedEventArgs(modelPage)},
                new object[] {nameof(Application.ModalPopped), new ModalPoppedEventArgs(modelPage)}
            };
        }
    }

    [Fact]
    public void Application_RequestedThemeChanged_AddsBreadcrumb()
    {
        // Arrange
        var application = MockApplication.Create();
        application.UserAppTheme = AppTheme.Unspecified;
        _fixture.Binder.HandleApplicationEvents(application);

        // Act
        application.UserAppTheme = AppTheme.Dark;

        // Assert
        var crumb = Assert.Single(_fixture.Scope.Breadcrumbs);
        Assert.Equal($"{nameof(MockApplication)}.{nameof(Application.RequestedThemeChanged)}", crumb.Message);
        Assert.Equal(BreadcrumbLevel.Info, crumb.Level);
        Assert.Equal(MauiEventsBinder.SystemType, crumb.Type);
        Assert.Equal(MauiEventsBinder.RenderingCategory, crumb.Category);
        crumb.Data.Should().Contain("RequestedTheme", AppTheme.Dark.ToString());
    }

    [Fact]
    public void Application_UnbindRequestedThemeChanged_DoesNotAddBreadcrumb()
    {
        // Arrange
        var application = MockApplication.Create();
        application.UserAppTheme = AppTheme.Unspecified;
        _fixture.Binder.HandleApplicationEvents(application);

        application.UserAppTheme = AppTheme.Dark;
        Assert.Single(_fixture.Scope.Breadcrumbs); // Sanity check

        _fixture.Binder.HandleApplicationEvents(application, bind: false);

        // Act
        application.UserAppTheme = AppTheme.Light;

        // Assert
        Assert.Single(_fixture.Scope.Breadcrumbs);
    }
}
