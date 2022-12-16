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
        _fixture.Binder.BindButtonEvents(button);

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
}
