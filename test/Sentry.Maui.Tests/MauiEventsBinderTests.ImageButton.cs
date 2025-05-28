using Sentry.Maui.Internal;

namespace Sentry.Maui.Tests;

public partial class MauiEventsBinderTests
{
    [Theory]
    [InlineData(nameof(ImageButton.Clicked))]
    [InlineData(nameof(ImageButton.Pressed))]
    [InlineData(nameof(ImageButton.Released))]
    public void ImageButton_CommonEvents_AddsBreadcrumb(string eventName)
    {
        // Arrange
        var button = new ImageButton
        {
            StyleId = "button"
        };
        var el = new ElementEventArgs(button);
        _fixture.Binder.OnApplicationOnDescendantAdded(null, el);

        // Act
        button.RaiseEvent(eventName, EventArgs.Empty);

        // Assert
        var crumb = Assert.Single(_fixture.Scope.Breadcrumbs);
        Assert.Equal($"{nameof(ImageButton)}.{eventName}", crumb.Message);
        Assert.Equal(BreadcrumbLevel.Info, crumb.Level);
        Assert.Equal(MauiEventsBinder.UserType, crumb.Type);
        Assert.Equal(MauiEventsBinder.UserActionCategory, crumb.Category);
        crumb.Data.Should().Contain($"{nameof(ImageButton)}.Name", "button");
    }

    [Theory]
    [InlineData(nameof(ImageButton.Clicked))]
    [InlineData(nameof(ImageButton.Pressed))]
    [InlineData(nameof(ImageButton.Released))]
    public void ImageButton_UnbindCommonEvents_DoesNotAddBreadcrumb(string eventName)
    {
        // Arrange
        var button = new ImageButton
        {
            StyleId = "button"
        };
        var el = new ElementEventArgs(button);
        _fixture.Binder.OnApplicationOnDescendantAdded(null, el);

        button.RaiseEvent(eventName, EventArgs.Empty);
        Assert.Single(_fixture.Scope.Breadcrumbs); // Sanity check

        _fixture.Binder.OnApplicationOnDescendantRemoved(null, el);

        // Act
        button.RaiseEvent(eventName, EventArgs.Empty);

        // Assert
        Assert.Single(_fixture.Scope.Breadcrumbs);
    }
}
