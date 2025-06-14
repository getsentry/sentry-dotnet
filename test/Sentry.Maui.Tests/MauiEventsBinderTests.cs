using Sentry.Maui.Internal;

namespace Sentry.Maui.Tests;

public partial class MauiEventsBinderTests
{
    private readonly MauiEventsBinderFixture _fixture = new();

    // Most of the tests for this class are in separate partial class files for better organisation

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
            Assert.Equal(breadcrumbEvent.ExtraData.Count(), crumb.Data.Count);
            foreach (var (key, value) in breadcrumbEvent.ExtraData)
            {
                crumb.Data.Should().Contain(kvp => kvp.Key == key && kvp.Value == value);
            }
        }
    }
}
