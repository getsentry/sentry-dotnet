using Sentry.Maui.Internal;

namespace Sentry.Maui.Tests;

public partial class MauiEventsBinderTests
{
    [Theory]
    [InlineData(nameof(Button.Clicked))]
    [InlineData(nameof(Button.Pressed))]
    [InlineData(nameof(Button.Released))]
    public void Button_CommonEvents_AddsBreadcrumb(string eventName)
    {
        // Arrange
        var button = new Button
        {
            StyleId = "button"
        };
        var el = new ElementEventArgs(button);
        _fixture.Binder.OnApplicationOnDescendantAdded(null, el);

        // Act
        button.RaiseEvent(eventName, EventArgs.Empty);

        // Assert
        var crumb = Assert.Single(_fixture.Scope.Breadcrumbs);
        Assert.Equal($"{nameof(Button)}.{eventName}", crumb.Message);
        Assert.Equal(BreadcrumbLevel.Info, crumb.Level);
        Assert.Equal(MauiEventsBinder.UserType, crumb.Type);
        Assert.Equal(MauiEventsBinder.UserActionCategory, crumb.Category);
        crumb.Data.Should().Contain($"{nameof(Button)}.Name", "button");
    }

    [Theory]
    [InlineData(nameof(Button.Clicked))]
    [InlineData(nameof(Button.Pressed))]
    [InlineData(nameof(Button.Released))]
    public void Button_UnbindCommonEvents_DoesNotAddBreadcrumb(string eventName)
    {
        // Arrange
        var button = new Button
        {
            StyleId = "button"
        };
        var el = new ElementEventArgs(button);
        _fixture.Binder.OnApplicationOnDescendantAdded(null, el);

        button.RaiseEvent(eventName, EventArgs.Empty);
        Assert.Single(_fixture.Scope.Breadcrumbs); // Sanity check

        // Act
        button.RaiseEvent(eventName, EventArgs.Empty);

        // Assert
        Assert.Single(_fixture.Scope.Breadcrumbs);
    }
}
