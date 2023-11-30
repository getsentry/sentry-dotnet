using Sentry.Maui.Internal;
using Sentry.Maui.Tests.Mocks;

namespace Sentry.Maui.Tests;

public partial class MauiEventsBinderTests
{
    [Theory]
    [InlineData(nameof(Element.ChildAdded))]
    [InlineData(nameof(Element.ChildRemoved))]
    public void Element_ChildEvents_AddsBreadcrumb(string eventName)
    {
        // Arrange
        var parent = new MockElement("parent");
        _fixture.Binder.HandleElementEvents(parent);

        var child = new MockElement("child");

        // Act
        parent.RaiseEvent(eventName, new ElementEventArgs(child));

        // Assert
        var crumb = Assert.Single(_fixture.Scope.Breadcrumbs);
        Assert.Equal($"{nameof(MockElement)}.{eventName}", crumb.Message);
        Assert.Equal(BreadcrumbLevel.Info, crumb.Level);
        Assert.Equal(MauiEventsBinder.SystemType, crumb.Type);
        Assert.Equal(MauiEventsBinder.RenderingCategory, crumb.Category);
        crumb.Data.Should().Contain($"{nameof(MockElement)}.Name", "parent");
        crumb.Data.Should().Contain("Element", nameof(MockElement));
        crumb.Data.Should().Contain("Element.Name", "child");
    }

    [Fact]
    public void Element_ParentChanged_AddsBreadcrumb()
    {
        // Arrange
        var element = new MockElement("element")
        {
            Parent = new MockElement("parent")
        };
        _fixture.Binder.HandleElementEvents(element);

        // Act
        element.RaiseEvent(nameof(Application.ParentChanged), EventArgs.Empty);

        // Assert
        var crumb = Assert.Single(_fixture.Scope.Breadcrumbs);
        Assert.Equal($"{nameof(MockElement)}.{nameof(Element.ParentChanged)}", crumb.Message);
        Assert.Equal(BreadcrumbLevel.Info, crumb.Level);
        Assert.Equal(MauiEventsBinder.SystemType, crumb.Type);
        Assert.Equal(MauiEventsBinder.RenderingCategory, crumb.Category);
        crumb.Data.Should().Contain($"{nameof(MockElement)}.Name", "element");
        crumb.Data.Should().Contain("Parent", element.Parent.GetType().Name);
        crumb.Data.Should().Contain("Parent.Name", element.Parent.StyleId);
    }

    [Fact]
    public void Element_BindingContextChanged_AddsBreadcrumb()
    {
        // Arrange
        var element = new MockElement("element");
        _fixture.Binder.HandleElementEvents(element);

        var bindingContext = Substitute.For<object>();

        // Act
        element.BindingContext = bindingContext;

        // Assert
        var crumb = Assert.Single(_fixture.Scope.Breadcrumbs);
        Assert.Equal($"{nameof(MockElement)}.{nameof(Element.BindingContextChanged)}", crumb.Message);
        Assert.Equal(BreadcrumbLevel.Info, crumb.Level);
        Assert.Equal(MauiEventsBinder.SystemType, crumb.Type);
        Assert.Equal(MauiEventsBinder.RenderingCategory, crumb.Category);
        crumb.Data.Should().Contain($"{nameof(MockElement)}.Name", "element");
        crumb.Data.Should().Contain("BindingContext", bindingContext.ToString());
    }
}
