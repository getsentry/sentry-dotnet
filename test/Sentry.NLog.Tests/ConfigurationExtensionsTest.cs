namespace Sentry.NLog.Tests;

public class ConfigurationExtensionsTest
{
    [Fact]
    public void AddSentry_Parameterless_DefaultTargetName()
    {
        var actual = new LoggingConfiguration().AddSentry();
        Assert.Equal(ConfigurationExtensions.DefaultTargetName, actual.AllTargets[0].Name);
    }

    [Fact]
    public void AddSentry_ConfigCallback_CallbackInvoked()
    {
        var actual = new LoggingConfiguration().AddSentry(o => o.MinimumEventLevel = LogLevel.Warn);
        var sentryTarget = Assert.IsType<SentryTarget>(actual.AllTargets[0]);
        Assert.Equal(LogLevel.Warn.ToString(), sentryTarget.MinimumEventLevel);
    }

    [Fact]
    public void AddSentry_TargetName_TargetNamed()
    {
        var actual = new LoggingConfiguration().AddSentry(targetName: "custom");
        Assert.Equal("custom", actual.AllTargets[0].Name);
    }

    [Fact]
    public void AddSentry_DefaultOptions_RuleStartsAtInfo()
    {
        var actual = new LoggingConfiguration().AddSentry();

        Assert.Equal(new[] { LogLevel.Info, LogLevel.Warn, LogLevel.Error, LogLevel.Fatal },
            Assert.Single(actual.LoggingRules).Levels);
    }

    [Theory]
    [InlineData("Trace", "Info", "Info", "Trace")]
    [InlineData("Info", "Debug", "Info", "Debug")]
    [InlineData("Info", "Info", "Trace", "Trace")]
    [InlineData("Off", "Off", "Warn", "Warn")]
    public void AddSentry_RuleStartsAtLowestMinimumLevel(string eventLevel, string breadcrumbLevel, string logLevel, string expected)
    {
        var actual = new LoggingConfiguration().AddSentry(o =>
        {
            o.MinimumEventLevel = LogLevel.FromString(eventLevel);
            o.MinimumBreadcrumbLevel = LogLevel.FromString(breadcrumbLevel);
            o.MinimumLogLevel = LogLevel.FromString(logLevel);
        });

        Assert.Equal(LogLevel.FromString(expected), Assert.Single(actual.LoggingRules).Levels.Min());
    }

    [Fact]
    public void AddSentry_AllMinimumLevelsNull_RuleHasNoLevels()
    {
        var actual = new LoggingConfiguration().AddSentry(o =>
        {
            o.MinimumEventLevel = null;
            o.MinimumBreadcrumbLevel = null;
            o.MinimumLogLevel = null;
        });

        Assert.Empty(Assert.Single(actual.LoggingRules).Levels);
    }

    [Fact]
    public void AddTag_SetToTarget()
    {
        var sut = new SentryNLogOptions();

        Layout layout = "b";
        sut.AddTag("a", layout);

        var tag = Assert.Single(sut.Tags);
        Assert.Equal("a", tag.Name);
        Assert.Equal(layout, tag.Layout);
    }
}
