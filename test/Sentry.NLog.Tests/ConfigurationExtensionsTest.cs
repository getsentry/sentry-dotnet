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
    public void AddSentry_NoMinLevel_RuleCoversAllLevels()
    {
        var actual = new LoggingConfiguration().AddSentry();

        var rule = Assert.Single(actual.LoggingRules);
        Assert.Equal(LogLevel.AllLoggingLevels, rule.Levels);
    }

    [Fact]
    public void AddSentry_MinLevel_RuleStartsAtMinLevel()
    {
        var actual = new LoggingConfiguration().AddSentry(minLevel: LogLevel.Info);

        var rule = Assert.Single(actual.LoggingRules);
        Assert.Equal(new[] { LogLevel.Info, LogLevel.Warn, LogLevel.Error, LogLevel.Fatal }, rule.Levels);
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
