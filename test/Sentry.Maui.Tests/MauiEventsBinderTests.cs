using Sentry.Maui.Internal;

namespace Sentry.Maui.Tests;

public partial class MauiEventsBinderTests
{
    private class Fixture
    {
        public IHub Hub { get; }

        public MauiEventsBinder Binder { get; }

        public Scope Scope { get; } = new();

        public SentryMauiOptions Options { get; } = new();

        public Fixture()
        {
            Hub = Substitute.For<IHub>();
            Hub.When(h => h.ConfigureScope(Arg.Any<Action<Scope>>()))
                .Do(c =>
                {
                    c.Arg<Action<Scope>>()(Scope);
                });

            Scope.Transaction = Substitute.For<ITransactionTracer>();

            Options.Debug = true;
            var logger = Substitute.For<IDiagnosticLogger>();
            logger.IsEnabled(Arg.Any<SentryLevel>()).Returns(true);
            Options.DiagnosticLogger = logger;
            var options = Microsoft.Extensions.Options.Options.Create(Options);
            Binder = new MauiEventsBinder(
                Hub,
                options,
                [
                    new MauiButtonEventsBinder(),
                    new MauiImageButtonEventsBinder(),
                    new MauiGestureRecognizerEventsBinder()
                ]
            );
        }
    }

    private readonly Fixture _fixture = new();

    // Tests are in partial class files for better organization

    [Fact]
    public void OnBreadcrumbCreateCallback_CreatesBreadcrumb()
    {
        // Arrange
        var breadcrumbEvent = new BreadcrumbEvent(new object(), "TestName",
            ("key1", "value1"), ("key2", "value2")
            );

        // Act
        _fixture.Binder.OnBreadcrumbCreateCallback(breadcrumbEvent);

        // Assert
        using (new AssertionScope())
        {
            var crumb = Assert.Single(_fixture.Scope.Breadcrumbs);
            Assert.Equal("Object.TestName", crumb.Message);
            Assert.Equal(BreadcrumbLevel.Info, crumb.Level);
            Assert.Equal(MauiEventsBinder.UserType, crumb.Type);
            Assert.Equal(MauiEventsBinder.UserActionCategory, crumb.Category);
            Assert.NotNull(crumb.Data);
            Assert.Equal(breadcrumbEvent.ExtraData.Length, crumb.Data.Count);
            foreach (var (key, value) in breadcrumbEvent.ExtraData)
            {
                crumb.Data.Should().Contain(kvp => kvp.Key == key && kvp.Value == value);
            }
        }
    }
}
