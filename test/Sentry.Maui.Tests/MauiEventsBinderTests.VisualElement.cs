using Sentry.Maui.Internal;
using Sentry.Maui.Tests.Mocks;

namespace Sentry.Maui.Tests;

public partial class MauiEventsBinderTests
{
    [Theory]
    [InlineData(nameof(VisualElement.Focused), true)]
    [InlineData(nameof(VisualElement.Unfocused), false)]
    public void VisualElement_FocusEvents_AddsBreadcrumb(string eventName, bool isFocused)
    {
        // Arrange
        var element = new MockVisualElement("element");
        MauiEventsBinder.HandleVisualElementEvents(element);

        // Act
        element.RaiseEvent(eventName, new FocusEventArgs(element, isFocused));

        // Assert
        var crumb = Assert.Single(_fixture.Scope.Breadcrumbs);
        Assert.Equal($"{nameof(MockVisualElement)}.{eventName}", crumb.Message);
        Assert.Equal(BreadcrumbLevel.Info, crumb.Level);
        Assert.Equal(MauiEvents.SystemType, crumb.Type);
        Assert.Equal(MauiEvents.RenderingCategory, crumb.Category);
        crumb.Data.Should().Contain($"{nameof(MockVisualElement)}.Name", "element");
    }
}
