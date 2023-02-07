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
        _fixture.Binder.BindElementEvents(parent);

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
    public void Element_ParentChanging_AddsBreadcrumb()
    {
        // Arrange
        var element = new MockElement("element");
        _fixture.Binder.BindElementEvents(element);

        var parent1 = new MockElement("parent1");
        var parent2 = new MockElement("parent2");

        // Act
        element.RaiseEvent(nameof(Application.ParentChanging), new ParentChangingEventArgs(parent1, parent2));

        // Assert
        var crumb = Assert.Single(_fixture.Scope.Breadcrumbs);
        Assert.Equal($"{nameof(MockElement)}.{nameof(Element.ParentChanging)}", crumb.Message);
        Assert.Equal(BreadcrumbLevel.Info, crumb.Level);
        Assert.Equal(MauiEventsBinder.SystemType, crumb.Type);
        Assert.Equal(MauiEventsBinder.RenderingCategory, crumb.Category);
        crumb.Data.Should().Contain($"{nameof(MockElement)}.Name", "element");
        crumb.Data.Should().Contain("OldParent", parent1.GetType().Name);
        crumb.Data.Should().Contain("OldParent.Name", parent1.StyleId);
        crumb.Data.Should().Contain("NewParent", parent2.GetType().Name);
        crumb.Data.Should().Contain("NewParent.Name", parent2.StyleId);
    }

    [Fact]
    public void Element_ParentChanged_AddsBreadcrumb()
    {
        // Arrange
        var element = new MockElement("element")
        {
            Parent = new MockElement("parent")
        };
        _fixture.Binder.BindElementEvents(element);

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
    public void Element_SetParent_AddsBreadcrumbs()
    {
        // Arrange
        var parent1 = new MockElement("parent1");
        var element = new MockElement("element")
        {
            Parent = parent1
        };

        _fixture.Binder.BindElementEvents(element);

        // Act
        var parent2 = new MockElement("parent2");
        element.Parent = parent2;

        // Assert
        Assert.Equal(2, _fixture.Scope.Breadcrumbs.Count);

        var crumb1 = _fixture.Scope.Breadcrumbs.ElementAt(0);
        Assert.Equal($"{nameof(MockElement)}.{nameof(Element.ParentChanging)}", crumb1.Message);
        Assert.Equal(BreadcrumbLevel.Info, crumb1.Level);
        Assert.Equal(MauiEventsBinder.SystemType, crumb1.Type);
        Assert.Equal(MauiEventsBinder.RenderingCategory, crumb1.Category);
        crumb1.Data.Should().Contain($"{nameof(MockElement)}.Name", "element");
        crumb1.Data.Should().Contain("OldParent", parent1.GetType().Name);
        crumb1.Data.Should().Contain("OldParent.Name", "parent1");
        crumb1.Data.Should().Contain("NewParent", parent2.GetType().Name);
        crumb1.Data.Should().Contain("NewParent.Name", "parent2");

        var crumb2 = _fixture.Scope.Breadcrumbs.ElementAt(1);
        Assert.Equal($"{nameof(MockElement)}.{nameof(Element.ParentChanged)}", crumb2.Message);
        Assert.Equal(BreadcrumbLevel.Info, crumb2.Level);
        Assert.Equal(MauiEventsBinder.SystemType, crumb2.Type);
        Assert.Equal(MauiEventsBinder.RenderingCategory, crumb2.Category);
        crumb2.Data.Should().Contain($"{nameof(MockElement)}.Name", "element");
        crumb2.Data.Should().Contain("Parent", parent2.GetType().Name);
        crumb2.Data.Should().Contain("Parent.Name", "parent2");
    }

    [Fact]
    public void Element_HandlerChanging_AddsBreadcrumb()
    {
        // Arrange
        var handler1 = Substitute.For<IElementHandler>();
        var element = new MockElement("element")
        {
            Handler = handler1
        };

        _fixture.Binder.BindElementEvents(element);
        var handler2 = Substitute.For<IElementHandler>();

        // Act
        element.RaiseEvent(nameof(Application.HandlerChanging), new HandlerChangingEventArgs(handler1, handler2));

        // Assert
        var crumb = Assert.Single(_fixture.Scope.Breadcrumbs);
        Assert.Equal($"{nameof(MockElement)}.{nameof(Element.HandlerChanging)}", crumb.Message);
        Assert.Equal(BreadcrumbLevel.Info, crumb.Level);
        Assert.Equal(MauiEventsBinder.SystemType, crumb.Type);
        Assert.Equal(MauiEventsBinder.HandlersCategory, crumb.Category);
        crumb.Data.Should().Contain($"{nameof(MockElement)}.Name", "element");
        crumb.Data.Should().Contain("OldHandler", handler1.ToString());
        crumb.Data.Should().Contain("NewHandler", handler2.ToString());
    }

    [Fact]
    public void Element_HandlerChanged_AddsBreadcrumb()
    {
        // Arrange
        var handler = Substitute.For<IElementHandler>();
        var element = new MockElement("element")
        {
            Handler = handler
        };

        _fixture.Binder.BindElementEvents(element);

        // Act
        element.RaiseEvent(nameof(Application.HandlerChanged), EventArgs.Empty);

        // Assert
        var crumb = Assert.Single(_fixture.Scope.Breadcrumbs);
        Assert.Equal($"{nameof(MockElement)}.{nameof(Element.HandlerChanged)}", crumb.Message);
        Assert.Equal(BreadcrumbLevel.Info, crumb.Level);
        Assert.Equal(MauiEventsBinder.SystemType, crumb.Type);
        Assert.Equal(MauiEventsBinder.HandlersCategory, crumb.Category);
        crumb.Data.Should().Contain($"{nameof(MockElement)}.Name", "element");
        crumb.Data.Should().Contain("Handler", handler.ToString());
    }

    [Fact]
    public void Element_SetHandler_AddsBreadcrumbs()
    {
        // Arrange
        var handler1 = Substitute.For<IElementHandler>();
        var element = new MockElement("element")
        {
            Handler = handler1
        };

        _fixture.Binder.BindElementEvents(element);
        var handler2 = Substitute.For<IElementHandler>();

        // Act
        element.Handler = handler2;

        // Assert
        Assert.Equal(2, _fixture.Scope.Breadcrumbs.Count);

        var crumb1 = _fixture.Scope.Breadcrumbs.ElementAt(0);
        Assert.Equal($"{nameof(MockElement)}.{nameof(Element.HandlerChanging)}", crumb1.Message);
        Assert.Equal(BreadcrumbLevel.Info, crumb1.Level);
        Assert.Equal(MauiEventsBinder.SystemType, crumb1.Type);
        Assert.Equal(MauiEventsBinder.HandlersCategory, crumb1.Category);
        crumb1.Data.Should().Contain($"{nameof(MockElement)}.Name", "element");
        crumb1.Data.Should().Contain("OldHandler", handler1.ToString());
        crumb1.Data.Should().Contain("NewHandler", handler2.ToString());

        var crumb2 = _fixture.Scope.Breadcrumbs.ElementAt(1);
        Assert.Equal($"{nameof(MockElement)}.{nameof(Element.HandlerChanged)}", crumb2.Message);
        Assert.Equal(BreadcrumbLevel.Info, crumb2.Level);
        Assert.Equal(MauiEventsBinder.SystemType, crumb2.Type);
        Assert.Equal(MauiEventsBinder.HandlersCategory, crumb2.Category);
        crumb2.Data.Should().Contain($"{nameof(MockElement)}.Name", "element");
        crumb2.Data.Should().Contain("Handler", handler2.ToString());
    }

    [Fact]
    public void Element_BindingContextChanged_AddsBreadcrumb()
    {
        // Arrange
        var element = new MockElement("element");
        _fixture.Binder.BindElementEvents(element);

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

    [Fact]
    public void Element_ReflectedEvent_AddsBreadcrumb()
    {
        // Arrange
        var element = new MockElement("element");
        _fixture.Binder.BindReflectedEvents(element);

        // Act
        element.RaiseCustomEvent();

        // Assert
        var crumb = Assert.Single(_fixture.Scope.Breadcrumbs);
        Assert.Equal($"{nameof(MockElement)}.{nameof(MockElement.CustomEvent)}", crumb.Message);
        Assert.Equal(BreadcrumbLevel.Info, crumb.Level);
        Assert.Null(crumb.Type);
        Assert.Null(crumb.Category);
        crumb.Data.Should().Contain($"{nameof(MockElement)}.Name", "element");
    }

    [Theory]
    [MemberData(nameof(GetReflectedEventTestData))]
    public void Element_ReflectedEvent_OmitsBreadcrumbForExplicitlyHandledType(Type type, bool isNegativeTest)
    {
        // Arrange
        var (target, eventName, eventArgs) = GetReflectedEventTestDataForType(type.Name);
        _fixture.Binder.BindReflectedEvents(target, includeExplicitlyHandledTypes: isNegativeTest);

        // Act
        target.RaiseEvent(eventName, eventArgs);

        // Assert
        if (isNegativeTest)
        {
            Assert.NotEmpty(_fixture.Scope.Breadcrumbs);
        }
        else
        {
            Assert.Empty(_fixture.Scope.Breadcrumbs);
        }
    }

    public static IEnumerable<object[]> GetReflectedEventTestData()
    {
        foreach (var type in MauiEventsBinder.ExplicitlyHandledTypes)
        {
            yield return new object[] {type, false};
            yield return new object[] {type, true};
        }
    }

    private static (BindableObject Target, string EventName, object EventArgs) GetReflectedEventTestDataForType
        (string typeName) =>

        // Each type in MauiEventsBinder.ExplicitlyHandledTypes should have an entry here.
        // The event should be one that is attached directly on that type (not a derived type).
        // The event also needs to be one whose delegate can be invoked directly via reflection.

        typeName switch
        {
            nameof(BindableObject) => (
                Substitute.For<BindableObject>(),
                nameof(BindableObject.BindingContextChanged),
                EventArgs.Empty),

            nameof(Element) => (
                Substitute.For<Element>(),
                nameof(Element.ParentChanged), EventArgs.Empty),

            nameof(VisualElement) => (
                Substitute.For<VisualElement>(),
                nameof(VisualElement.ChildrenReordered),
                EventArgs.Empty),

            nameof(Application) => (
                MockApplication.Create(),
                nameof(Application.ModalPushed),
                new ModalPushedEventArgs(Substitute.For<Page>())),

            nameof(Window) => (
                Substitute.For<Window>(),
                nameof(Window.Activated),
                EventArgs.Empty),

            nameof(Shell) => (
                Substitute.For<Shell>(),
                nameof(Shell.Navigated),
                new ShellNavigatedEventArgs(Substitute.For<ShellNavigationState>(),
                    Substitute.For<ShellNavigationState>(), ShellNavigationSource.Unknown)),

            nameof(Page) => (
                Substitute.For<Page>(),
                nameof(Page.Appearing),
                EventArgs.Empty),

            nameof(Button) => (
                Substitute.For<Button>(),
                nameof(Button.Clicked),
                EventArgs.Empty),

            _ => throw new NotImplementedException()
        };
}
