namespace Sentry.EntityFramework.Tests;

public class SentryQueryLoggerTests
{
    private static (IHub Hub, Scope Scope) GetHub()
    {
        var scope = new Scope(new SentryOptions());
        var hub = Substitute.For<IHub>();
        hub.IsEnabled.Returns(true);
        hub.SubstituteConfigureScope(scope);
        return (hub, scope);
    }

    [Fact]
    public void Log_QueryLogger_CaptureEvent()
    {
        var (hub, scope) = GetHub();

        var expected = new
        {
            Query = "Expected query string",
            Level = BreadcrumbLevel.Debug,
            Category = "Entity Framework"
        };

        var logger = new SentryQueryLogger(new SentryOptions(), hub);

        logger.Log(expected.Query);

        var b = scope.Breadcrumbs.First();
        Assert.Equal(expected.Query, b.Message);
        Assert.Equal(expected.Category, b.Category);
        Assert.Equal(expected.Level, b.Level);
    }

    [Theory]
    [MemberData(nameof(DatabaseQueryDataCases.SendDefaultPiiAndConfiguredValue_Collected), MemberType = typeof(DatabaseQueryDataCases))]
    public void Log_Query_BreadcrumbFollowsDatabaseQueryData(bool sendDefaultPii, bool? databaseQueryData, bool collected)
    {
        var (hub, scope) = GetHub();
        var options = new SentryOptions().WithDatabaseQueryData(sendDefaultPii, databaseQueryData);
        var logger = new SentryQueryLogger(options, hub);

        logger.Log("SELECT * FROM rentals WHERE name = 'Jane'");

        scope.Breadcrumbs.Should().HaveCount(collected ? 1 : 0);
    }
}
