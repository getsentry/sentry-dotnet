using Sentry.Maui.Internal;

namespace Sentry.Maui.Tests;

public partial class MauiEventsBinderTests
{
    [Theory]
    [InlineData(nameof(Page.Appearing))]
    [InlineData(nameof(Page.Disappearing))]
    public void Page_LifecycleEvents_AddsBreadcrumb(string eventName)
    {
        // Arrange
        var page = new Page
        {
            StyleId = "page"
        };
        _fixture.Binder.HandlePageEvents(page);

        // Act
        page.RaiseEvent(eventName, EventArgs.Empty);

        // Assert
        var crumb = Assert.Single(_fixture.Scope.Breadcrumbs);
        Assert.Equal($"{nameof(Page)}.{eventName}", crumb.Message);
        Assert.Equal(BreadcrumbLevel.Info, crumb.Level);
        Assert.Equal(MauiEventsBinder.SystemType, crumb.Type);
        Assert.Equal(MauiEventsBinder.LifecycleCategory, crumb.Category);
        crumb.Data.Should().Contain($"{nameof(Page)}.Name", "page");
    }

    [Theory]
    [InlineData(nameof(Page.Appearing))]
    [InlineData(nameof(Page.Disappearing))]
    public void Page_UnbindLifecycleEvents_DoesNotAddBreadcrumb(string eventName)
    {
        // Arrange
        var page = new Page
        {
            StyleId = "page"
        };
        _fixture.Binder.HandlePageEvents(page);

        page.RaiseEvent(eventName, EventArgs.Empty);
        Assert.Single(_fixture.Scope.Breadcrumbs); // Sanity check

        _fixture.Binder.HandlePageEvents(page, bind: false);

        // Act
        page.RaiseEvent(eventName, EventArgs.Empty);

        // Assert
        Assert.Single(_fixture.Scope.Breadcrumbs);
    }

    [Fact]
    public void Page_NavigatedTo_AddsBreadcrumb()
    {
        // Arrange
        var page = new Page
        {
            StyleId = "page"
        };
        _fixture.Binder.HandlePageEvents(page);

        var otherPage = new Page
        {
            StyleId = "otherPage"
        };
        var navigatedToEventArgs = (NavigatedToEventArgs)
            typeof(NavigatedToEventArgs)
                .GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, new[] {typeof(Page)})!
                .Invoke(new object[] {otherPage});

        // Act
        page.RaiseEvent(nameof(Page.NavigatedTo), navigatedToEventArgs);

        // Assert
        var crumb = Assert.Single(_fixture.Scope.Breadcrumbs);
        Assert.Equal($"{nameof(Page)}.{nameof(Page.NavigatedTo)}", crumb.Message);
        Assert.Equal(BreadcrumbLevel.Info, crumb.Level);
        Assert.Equal(MauiEventsBinder.NavigationType, crumb.Type);
        Assert.Equal(MauiEventsBinder.NavigationCategory, crumb.Category);
        crumb.Data.Should().Contain($"{nameof(Page)}.Name", "page");
        crumb.Data.Should().Contain("PreviousPage", nameof(Page));
        crumb.Data.Should().Contain("PreviousPage.Name", "otherPage");
    }

    [Fact]
    public void Page_UnbindNavigatedTo_AddsBreadcrumb()
    {
        // Arrange
        var page = new Page
        {
            StyleId = "page"
        };
        _fixture.Binder.HandlePageEvents(page);

        var otherPage = new Page
        {
            StyleId = "otherPage"
        };
        var navigatedToEventArgs = (NavigatedToEventArgs)
            typeof(NavigatedToEventArgs)
                .GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, new[] {typeof(Page)})!
                .Invoke(new object[] {otherPage});

        page.RaiseEvent(nameof(Page.NavigatedTo), navigatedToEventArgs);
        Assert.Single(_fixture.Scope.Breadcrumbs); // Sanity check

        _fixture.Binder.HandlePageEvents(page, bind: false);

        // Act
        page.RaiseEvent(nameof(Page.NavigatedTo), navigatedToEventArgs);

        // Assert
        Assert.Single(_fixture.Scope.Breadcrumbs);
    }

    [Fact]
    public void Page_LayoutChanged_AddsBreadcrumb()
    {
        // Arrange
        var page = new Page
        {
            StyleId = "page"
        };
        _fixture.Binder.HandlePageEvents(page);

        // Act
        page.RaiseEvent(nameof(Page.LayoutChanged), EventArgs.Empty);

        // Assert
        var crumb = Assert.Single(_fixture.Scope.Breadcrumbs);
        Assert.Equal($"{nameof(Page)}.{nameof(Page.LayoutChanged)}", crumb.Message);
        Assert.Equal(BreadcrumbLevel.Info, crumb.Level);
        Assert.Equal(MauiEventsBinder.SystemType, crumb.Type);
        Assert.Equal(MauiEventsBinder.RenderingCategory, crumb.Category);
        crumb.Data.Should().Contain($"{nameof(Page)}.Name", "page");
    }
}
