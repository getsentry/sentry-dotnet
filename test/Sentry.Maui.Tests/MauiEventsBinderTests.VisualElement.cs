using Sentry.Maui.Internal;
using Sentry.Maui.Tests.Mocks;

namespace Sentry.Maui.Tests;

public partial class MauiEventsBinderTests
{
    [Theory]
    [InlineData(nameof(VisualElement.Loaded), "_loaded")]
    [InlineData(nameof(VisualElement.Unloaded), "_unloaded")]
    [InlineData(nameof(VisualElement.ChildrenReordered))]
    [InlineData(nameof(VisualElement.MeasureInvalidated))]
    [InlineData(nameof(VisualElement.SizeChanged))]
    public void VisualElement_CommonEvents_AddsBreadcrumb(string eventName, string delegateName = default)
    {
        // Arrange
        var element = new MockVisualElement("element");
        _fixture.Binder.BindVisualElementEvents(element);

        // Act
        element.RaiseEvent(delegateName ?? eventName, EventArgs.Empty);

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
    public void VisualElement_FocusEvents_AddsBreadcrumb(string eventName, bool isFocused)
    {
        // Arrange
        var element = new MockVisualElement("element");
        _fixture.Binder.BindVisualElementEvents(element);

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
}
