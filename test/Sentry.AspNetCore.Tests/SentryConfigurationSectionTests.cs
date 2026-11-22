#if NET6_0_OR_GREATER
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;
using Sentry.Internal;

namespace Sentry.AspNetCore.Tests;

[Collection(nameof(SentrySdkCollection))]
public class SentryConfigurationSectionTests : IDisposable
{
    public void Dispose() => SentrySdk.Close();

    private static SentryAspNetCoreOptions BuildOptions(params (string Key, string Value)[] settings)
        => BuildOptions(null, settings);

    private static SentryAspNetCoreOptions BuildOptions(
        Action<WebApplicationBuilder> configure,
        params (string Key, string Value)[] settings)
    {
        var builder = WebApplication.CreateBuilder();
        ((IConfigurationBuilder)builder.Configuration).AddInMemoryCollection(
            settings.ToDictionary(s => s.Key, s => s.Value));
        configure?.Invoke(builder);
        builder.WebHost.UseSentry((SentryAspNetCoreOptions options) =>
        {
            options.Dsn = ValidDsn;
            options.BackgroundWorker = Substitute.For<IBackgroundWorker>();
            options.AutoSessionTracking = false;
            options.InitNativeSdks = false;
        });

        using var app = builder.Build();
        return app.Services.GetRequiredService<IOptions<SentryAspNetCoreOptions>>().Value;
    }

    [Fact]
    public void SentrySection_ConfiguresSdkAndLoggingSettings()
    {
        var options = BuildOptions(
            ("Sentry:Release", "1.0.0"),
            ("Sentry:MinimumEventLevel", "Critical"));

        Assert.Equal("1.0.0", options.Release);
        Assert.Equal(LogLevel.Critical, options.MinimumEventLevel);
    }

    [Fact]
    public void LoggingSection_ConfiguresLoggingSettings()
    {
        var options = BuildOptions(
            ("Logging:Sentry:MinimumEventLevel", "Critical"),
            ("Logging:Sentry:MinimumBreadcrumbLevel", "Warning"));

        Assert.Equal(LogLevel.Critical, options.MinimumEventLevel);
        Assert.Equal(LogLevel.Warning, options.MinimumBreadcrumbLevel);
    }

    [Fact]
    public void LoggingSection_SdkSetting_Throws()
    {
        var exception = Assert.Throws<NotSupportedException>(
            () => BuildOptions(("Logging:Sentry:Dsn", ValidDsn)));

        Assert.Contains("Dsn", exception.Message);
    }

    [Fact]
    public void LoggingAddConfiguration_WholeConfiguration_SentrySectionNotRejected()
    {
        var options = BuildOptions(
            builder => builder.Logging.AddConfiguration(builder.Configuration),
            ("Sentry:Release", "1.0.0"),
            ("Logging:Sentry:MinimumEventLevel", "Critical"));

        Assert.Equal("1.0.0", options.Release);
        Assert.Equal(LogLevel.Critical, options.MinimumEventLevel);
    }

    [Fact]
    public void SentrySection_AppliedOnce()
    {
        var options = BuildOptions(("Sentry:Release", "1.0.0"));

        Assert.Single(options.GetAllTransactionProcessors().OfType<TraceIgnoreStatusCodeTransactionProcessor>());
        Assert.Single(options.Logging.Filters);
    }
}
#endif
