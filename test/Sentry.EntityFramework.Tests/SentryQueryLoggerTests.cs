namespace Sentry.EntityFramework.Tests;

public class SentryQueryLoggerTests : IDisposable
{
    public void Dispose() => SentryClientExtensions.SentryOptionsForTestingOnly = null;

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

    [Theory]
    [InlineData(false, "SELECT * FROM Users WHERE Email = 'jane@example.com'")]
    [InlineData(true, "SELECT * FROM Users WHERE Email = ?")]
    public void Log_DataCollection_SanitizesOnlyWhenConfigured(bool configureDataCollection, string expectedMessage)
    {
        var options = new SentryOptions();
        if (configureDataCollection)
        {
            options.ConfigureDataCollection(_ => { });
        }
        SentryClientExtensions.SentryOptionsForTestingOnly = options;

        var scope = new Scope(options);
        var hub = Substitute.For<IHub>();
        hub.IsEnabled.Returns(true);
        hub.SubstituteConfigureScope(scope);

        new SentryQueryLogger(hub).Log("SELECT * FROM Users WHERE Email = 'jane@example.com'");

        scope.Breadcrumbs.Single().Message.Should().Be(expectedMessage);
    }

    [Fact]
    public void Log_MySqlCommandThroughInterceptor_UsesMySqlQuoting()
    {
        var options = new SentryOptions();
        options.ConfigureDataCollection(_ => { });
        SentryClientExtensions.SentryOptionsForTestingOnly = options;

        var scope = new Scope(options);
        var hub = Substitute.For<IHub>();
        hub.IsEnabled.Returns(true);
        hub.SubstituteConfigureScope(scope);
        var interceptor = new SentryCommandInterceptor(new SentryQueryLogger(hub));
        var command = new MySql.Data.MySqlClient.FakeMySqlCommand { CommandText = "SELECT * FROM Users WHERE Name = 'O\\'Brien'" };

        interceptor.ReaderExecuting(command, new DbCommandInterceptionContext<DbDataReader>());

        scope.Breadcrumbs.Single().Message.Should().Be("SELECT * FROM Users WHERE Name = ?");
    }
}
