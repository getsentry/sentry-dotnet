#nullable enable

using System.Xml;
using log4net.Config;
using log4net.Util;

namespace Sentry.Log4Net.Tests;

public class SentryAppenderConfigurationBindingTests
{
    private static (Exception? Exception, int AppenderCount, string Errors) LoadConfiguration(string appenderElements)
    {
        var configXml = $"""
            <log4net>
              <appender name="sentry" type="{typeof(SentryAppender).AssemblyQualifiedName}">
                {appenderElements}
              </appender>
              <root><level value="DEBUG" /><appender-ref ref="sentry" /></root>
            </log4net>
            """;

        var document = new XmlDocument();
        document.LoadXml(configXml);

        var errors = new List<string>();
        void OnLogReceived(object? sender, LogReceivedEventArgs e)
        {
            if (e.LogLog.Exception is { } exception)
            {
                errors.Add($"{e.LogLog.Message} {exception.Message}");
            }
            else
            {
                errors.Add(e.LogLog.Message);
            }
        }

        LogLog.LogReceived += OnLogReceived;
        try
        {
            var repository = LogManager.CreateRepository(Guid.NewGuid().ToString());
            var exception = Record.Exception(() => XmlConfigurator.Configure(repository, document.DocumentElement));
            return (exception, repository.GetAppenders().Length, string.Join(" | ", errors));
        }
        finally
        {
            LogLog.LogReceived -= OnLogReceived;
        }
    }

    [Fact]
    public void LoadConfiguration_WithDsn_ReportsMigrationError()
    {
        var (exception, appenderCount, errors) = LoadConfiguration("""<Dsn value="https://key@sentry.io/1" />""");

        // log4net catches exceptions thrown while setting a parameter, so the config still loads and the appender attaches.
        Assert.Null(exception);
        Assert.Equal(1, appenderCount);
        Assert.Contains("SentrySdk.Init", errors);
    }

    [Fact]
    public void LoadConfiguration_WithAppenderSettings_ReportsNoErrors()
    {
        var (exception, appenderCount, errors) = LoadConfiguration(
            """<SendIdentity value="true" /><MinimumEventLevel value="ERROR" />""");

        Assert.Null(exception);
        Assert.Equal(1, appenderCount);
        Assert.Empty(errors);
    }

    [Fact]
    public void Dsn_WhenSet_Throws()
    {
        var exception = Assert.Throws<TargetInvocationException>(
            () => DsnProperty.SetValue(new SentryAppender(), "https://key@sentry.io/1"));

        Assert.IsType<NotSupportedException>(exception.InnerException);
        Assert.Contains("SentrySdk.Init", exception.InnerException!.Message);
    }

    [Fact]
    public void Dsn_IsObsoleteAsError()
    {
        var obsolete = DsnProperty.GetCustomAttribute<ObsoleteAttribute>();

        Assert.NotNull(obsolete);
        Assert.True(obsolete!.IsError);
    }

    private static PropertyInfo DsnProperty => typeof(SentryAppender).GetProperty("Dsn")!;
}
