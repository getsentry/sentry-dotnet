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
        const string message = $"{nameof(MockApplication)}.{nameof(Element.ChildAdded)}";
        Assert.Equal(message, crumb.Message);
        Assert.Equal(BreadcrumbLevel.Info, crumb.Level);
        Assert.Equal(MauiEventsBinder.SystemType, crumb.Type);
        Assert.Equal(MauiEventsBinder.RenderingCategory, crumb.Category);
        crumb.Data.Should().Contain(nameof(Element), element.GetType().Name);
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
        crumb.Data.Should().Contain(nameof(Element), element.GetType().Name);
    }
}
