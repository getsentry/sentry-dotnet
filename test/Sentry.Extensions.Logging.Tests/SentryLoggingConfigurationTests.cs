using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Sentry.Extensions.Logging.Tests;

public class SentryLoggingConfigurationTests
{
    private static IConfiguration Section(params (string Key, string Value)[] settings)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(settings.ToDictionary(s => s.Key, s => s.Value))
            .Build();

    [Fact]
    public void ApplyTo_MinimumLevels_Applied()
    {
        var options = new SentryLoggingOptions();

        SentryLoggingConfiguration.ApplyTo(
            Section(("MinimumEventLevel", "Critical"), ("MinimumBreadcrumbLevel", "Warning")),
            options);

        Assert.Equal(LogLevel.Critical, options.MinimumEventLevel);
        Assert.Equal(LogLevel.Warning, options.MinimumBreadcrumbLevel);
    }

    [Fact]
    public void ApplyTo_LogLevelRules_Ignored()
    {
        var options = new SentryLoggingOptions();

        SentryLoggingConfiguration.ApplyTo(Section(("LogLevel:Default", "Warning")), options);

        Assert.Equal(LogLevel.Error, options.MinimumEventLevel);
    }

    [Fact]
    public void ApplyTo_EveryLoggingOption_IsRecognised()
    {
        var settings = typeof(SentryLoggingOptions).GetProperties()
            .Where(p => p.SetMethod?.IsPublic == true && p.GetCustomAttribute<ObsoleteAttribute>() is null)
            .Select(p => (p.Name, nameof(LogLevel.Warning)))
            .ToArray();

        SentryLoggingConfiguration.ApplyTo(Section(settings), new SentryLoggingOptions());
    }

    [Fact]
    public void ApplyTo_SdkSetting_ThrowsNamingIt()
    {
        var exception = Assert.Throws<NotSupportedException>(
            () => SentryLoggingConfiguration.ApplyTo(Section(("Release", "1.0.0")), new SentryLoggingOptions()));

        Assert.Contains("Release", exception.Message);
        Assert.Contains("'Sentry' configuration section", exception.Message);
    }

    [Fact]
    public void ApplyTo_SdkSettings_ThrowsNamingThemAll()
    {
        var exception = Assert.Throws<NotSupportedException>(
            () => SentryLoggingConfiguration.ApplyTo(
                Section(("Release", "1.0.0"), ("TracesSampleRate", "1.0")),
                new SentryLoggingOptions()));

        Assert.Contains("Release", exception.Message);
        Assert.Contains("TracesSampleRate", exception.Message);
    }

    [Theory]
    [InlineData("Dsn", "https://key@sentry.io/1")]
    [InlineData("InitializeSdk", "true")]
    public void ApplyTo_SdkInitializationSetting_ThrowsPointingAtInit(string key, string value)
    {
        var exception = Assert.Throws<NotSupportedException>(
            () => SentryLoggingConfiguration.ApplyTo(Section((key, value)), new SentryLoggingOptions()));

        Assert.Contains("SentrySdk.Init", exception.Message);
    }

    [Fact]
    public void ApplyTo_InitializeSdkFalse_Accepted()
        => SentryLoggingConfiguration.ApplyTo(Section(("InitializeSdk", "false")), new SentryLoggingOptions());
}
