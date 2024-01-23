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
        _fixture.Binder.HandleVisualElementEvents(element);

        // Act
        element.RaiseEvent(eventName, new FocusEventArgs(element, isFocused));

        // Assert
        var crumb = Assert.Single(_fixture.Scope.Breadcrumbs);
        Assert.Equal($"{nameof(MockVisualElement)}.{eventName}", crumb.Message);
        Assert.Equal(BreadcrumbLevel.Info, crumb.Level);
        Assert.Equal(MauiEventsBinder.SystemType, crumb.Type);
        Assert.Equal(MauiEventsBinder.RenderingCategory, crumb.Category);
        crumb.Data.Should().Contain($"{nameof(MockVisualElement)}.Name", "element");
    }

    [Theory]
    [InlineData(nameof(VisualElement.Focused), true)]
    [InlineData(nameof(VisualElement.Unfocused), false)]
    public void VisualElement_UnbindFocusEvents_DoesNotAddBreadcrumb(string eventName, bool isFocused)
    {
        // Arrange
        var element = new MockVisualElement("element");
        _fixture.Binder.HandleVisualElementEvents(element);

        element.RaiseEvent(eventName, new FocusEventArgs(element, isFocused));
        Assert.Single(_fixture.Scope.Breadcrumbs); // Sanity check

        _fixture.Binder.HandleVisualElementEvents(element, bind: false);

        // Act
        element.RaiseEvent(eventName, new FocusEventArgs(element, isFocused));

        // Assert
        Assert.Single(_fixture.Scope.Breadcrumbs);
    }
}
