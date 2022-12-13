using Microsoft.Extensions.Options;
using Sentry.Maui.Internal;
using Sentry.Maui.Tests.Mocks;

namespace Sentry.Maui.Tests;

public class MauiEventsBinderTests
{
    private class Fixture
    {
        public IMauiEventsBinder Binder { get; }

        public Scope Scope { get; } = new();

        public Fixture()
        {
            var hub = Substitute.For<IHub>();
            hub.When(h => h.ConfigureScope(Arg.Any<Action<Scope>>()))
                .Do(c => c.Arg<Action<Scope>>()(Scope));

            var options = Options.Create(new SentryMauiOptions());
            Binder = new MauiEventsBinder(hub, options);
        }
    }

    private readonly Fixture _fixture = new();

    [Fact]
    public void Application_ChildAdded_AddsBreadcrumb()
    {
        // Arrange
        var application = MockApplication.Create();
        _fixture.Binder.BindApplicationEvents(application);

        var element = Substitute.For<Element>();

        // Act
        application.InvokeOnChildAdded(element);

        // Assert
        var crumb = Assert.Single(_fixture.Scope.Breadcrumbs);
        Assert.Equal($"{nameof(MockApplication)}.{nameof(Element.ChildAdded)}", crumb.Message);
        Assert.Equal(BreadcrumbLevel.Info, crumb.Level);
        Assert.Equal(MauiEventsBinder.SystemType, crumb.Type);
        Assert.Equal(MauiEventsBinder.RenderingCategory, crumb.Category);
        crumb.Data.Should().Contain("Element", element.ToString());
    }

    [Fact]
    public void Application_ChildRemoved_AddsBreadcrumb()
    {
        // Arrange
        var application = MockApplication.Create();
        _fixture.Binder.BindApplicationEvents(application);

        var element = Substitute.For<Element>();

        // Act
        application.InvokeOnChildRemoved(element, 0);

        // Assert
        var crumb = Assert.Single(_fixture.Scope.Breadcrumbs);

        // Test partial string only, due to:
        // https://github.com/dotnet/maui/issues/11720
        Assert.EndsWith(nameof(Element.ChildRemoved), crumb.Message);

        Assert.Equal(BreadcrumbLevel.Info, crumb.Level);
        Assert.Equal(MauiEventsBinder.SystemType, crumb.Type);
        Assert.Equal(MauiEventsBinder.RenderingCategory, crumb.Category);
        crumb.Data.Should().Contain("Element", element.ToString());
    }

    [Fact]
    public void Element_ChildAdded_AddsBreadcrumb()
    {
        // Arrange
        var parent = new MockElement("parent");
        _fixture.Binder.BindElementEvents(parent);

        var child = new MockElement("child");

        // Act
        parent.InvokeOnChildAdded(child);

        // Assert
        var crumb = Assert.Single(_fixture.Scope.Breadcrumbs);
        Assert.Equal($"{nameof(MockElement)}.{nameof(Element.ChildAdded)}", crumb.Message);
        Assert.Equal(BreadcrumbLevel.Info, crumb.Level);
        Assert.Equal(MauiEventsBinder.SystemType, crumb.Type);
        Assert.Equal(MauiEventsBinder.RenderingCategory, crumb.Category);
        crumb.Data.Should().Contain($"{nameof(MockElement)}.Name", "parent");
        crumb.Data.Should().Contain("Element", nameof(MockElement));
        crumb.Data.Should().Contain("Element.Name", "child");
    }

    [Fact]
    public void Element_ChildRemoved_AddsBreadcrumb()
    {
        // Arrange
        var parent = new MockElement("parent");
        _fixture.Binder.BindElementEvents(parent);

        var child = new MockElement("child");

        // Act
        parent.InvokeOnChildRemoved(child, 0);

        // Assert
        var crumb = Assert.Single(_fixture.Scope.Breadcrumbs);

        // Test partial string only, due to:
        // https://github.com/dotnet/maui/issues/11720
        Assert.EndsWith(nameof(Element.ChildRemoved), crumb.Message);

        Assert.Equal(BreadcrumbLevel.Info, crumb.Level);
        Assert.Equal(MauiEventsBinder.SystemType, crumb.Type);
        Assert.Equal(MauiEventsBinder.RenderingCategory, crumb.Category);

        // Uncomment after issue fixed
        // https://github.com/dotnet/maui/issues/11720
        // crumb.Data.Should().Contain($"{nameof(MockElement)}.Name", "parent");

        crumb.Data.Should().Contain("Element", nameof(MockElement));
        crumb.Data.Should().Contain("Element.Name", "child");
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
    public void Element_SetHandler_AddsBreadcrumbs()
    {
        // Arrange
        var oldHandler = Substitute.For<IElementHandler>();
        var element = new MockElement("element")
        {
            Handler = oldHandler
        };

        _fixture.Binder.BindElementEvents(element);
        var newHandler = Substitute.For<IElementHandler>();

        // Act
        element.Handler = newHandler;

        // Assert
        Assert.Equal(2, _fixture.Scope.Breadcrumbs.Count);

        var crumb1 = _fixture.Scope.Breadcrumbs.ElementAt(0);
        Assert.Equal($"{nameof(MockElement)}.{nameof(Element.HandlerChanging)}", crumb1.Message);
        Assert.Equal(BreadcrumbLevel.Info, crumb1.Level);
        Assert.Equal(MauiEventsBinder.SystemType, crumb1.Type);
        Assert.Equal(MauiEventsBinder.HandlersCategory, crumb1.Category);
        crumb1.Data.Should().Contain($"{nameof(MockElement)}.Name", "element");
        crumb1.Data.Should().Contain("OldHandler", oldHandler.ToString());
        crumb1.Data.Should().Contain("NewHandler", newHandler.ToString());

        var crumb2 = _fixture.Scope.Breadcrumbs.ElementAt(1);
        Assert.Equal($"{nameof(MockElement)}.{nameof(Element.HandlerChanged)}", crumb2.Message);
        Assert.Equal(BreadcrumbLevel.Info, crumb2.Level);
        Assert.Equal(MauiEventsBinder.SystemType, crumb2.Type);
        Assert.Equal(MauiEventsBinder.HandlersCategory, crumb2.Category);
        crumb2.Data.Should().Contain($"{nameof(MockElement)}.Name", "element");
        crumb2.Data.Should().Contain("Handler", newHandler.ToString());
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
}
