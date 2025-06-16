namespace Sentry.EntityFramework.Tests;

public class SentryQueryLoggerTests
{
    [Fact]
    public void Log_QueryLogger_CaptureEvent()
    {
        var scope = new Scope(new SentryOptions());
        var hub = Substitute.For<IHub>();
        hub.IsEnabled.Returns(true);
        hub.SubstituteConfigureScope(scope);

        var expected = new
        {
            Query = "Expected query string",
            Level = BreadcrumbLevel.Debug,
            Category = "Entity Framework"
        };

        var logger = new SentryQueryLogger(hub);

        logger.Log(expected.Query);

        var b = scope.Breadcrumbs.First();
        Assert.Equal(expected.Query, b.Message);
        Assert.Equal(expected.Category, b.Category);
        Assert.Equal(expected.Level, b.Level);
    }
}
