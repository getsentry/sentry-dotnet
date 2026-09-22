#nullable enable

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Sentry.Extensions.Logging.Tests;

public class SentryLoggingOptionsConfigurationBindingTests
{
    private static Exception? BindConfiguration(string key, string value)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [key] = value })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConfiguration(config).AddSentry());
        using var provider = services.BuildServiceProvider();

        return Record.Exception(() => provider.GetRequiredService<IOptions<SentryLoggingOptions>>().Value);
    }

    [Theory]
    [InlineData("Sentry:Dsn", "https://key@sentry.io/1")]
    [InlineData("Sentry:InitializeSdk", "true")]
    [InlineData("Sentry:InitializeSdk", "false")]
    public void BindConfiguration_WithSdkSetting_Throws(string key, string value)
    {
        var exception = BindConfiguration(key, value);

        Assert.NotNull(exception);
        Assert.Contains("SentrySdk.Init", exception.ToString());
    }

    [Fact]
    public void BindConfiguration_WithLoggingSetting_DoesNotThrow()
        => Assert.Null(BindConfiguration("Sentry:MinimumEventLevel", nameof(LogLevel.Warning)));

    [Theory]
    [InlineData("Dsn", "https://key@sentry.io/1")]
    [InlineData("InitializeSdk", true)]
    public void SetProperty_SdkSetting_Throws(string propertyName, object value)
    {
        var property = typeof(SentryLoggingOptions).GetProperty(propertyName)!;

        var exception = Assert.Throws<TargetInvocationException>(
            () => property.SetValue(new SentryLoggingOptions(), value));

        Assert.IsType<NotSupportedException>(exception.InnerException);
        Assert.Contains("SentrySdk.Init", exception.InnerException!.Message);
    }

    private static MethodInfo DsnOverload => typeof(SentryLoggingOptions).Assembly
        .GetType("Microsoft.Extensions.Logging.LoggingBuilderExtensions")!
        .GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Single(m => m.Name == "AddSentry" && m.GetParameters().Any(p => p.Name == "dsn"));

    [Fact]
    public void AddSentry_DsnOverload_InvokedByName_Throws()
    {
        var exception = Assert.Throws<TargetInvocationException>(
            () => { DsnOverload.Invoke(null, [Substitute.For<ILoggingBuilder>(), "https://key@sentry.io/1"]); });

        Assert.IsType<NotSupportedException>(exception.InnerException);
        Assert.Contains("SentrySdk.Init", exception.InnerException!.Message);
    }

    [Theory]
    [InlineData("Dsn")]
    [InlineData("InitializeSdk")]
    public void SdkSettings_AreObsoleteAsError(string propertyName)
    {
        var obsolete = typeof(SentryLoggingOptions).GetProperty(propertyName)!.GetCustomAttribute<ObsoleteAttribute>();

        Assert.NotNull(obsolete);
        Assert.True(obsolete!.IsError);
    }

    [Fact]
    public void AddSentry_DsnOverload_IsObsoleteAsError()
    {
        var obsolete = DsnOverload.GetCustomAttribute<ObsoleteAttribute>();

        Assert.NotNull(obsolete);
        Assert.True(obsolete!.IsError);
    }
}
