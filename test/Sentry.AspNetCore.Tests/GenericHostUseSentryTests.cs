#nullable enable
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sentry.Extensions.Hosting;

namespace Sentry.AspNetCore.Tests;

[Collection(nameof(SentrySdkCollection))]
public class GenericHostUseSentryTests : IDisposable
{
    private const string HostedServiceCategory = "Sentry.Extensions.Hosting.SentryHostedService";

    private readonly List<SentryEvent> _events = new();
    private readonly List<SentryLog> _logs = new();
    private readonly CapturingLoggerProvider _hostLogs = new();

    public void Dispose() => SentrySdk.Close();

    private void DoNotSend(SentryOptions options)
    {
        options.Dsn = ValidDsn;
        options.BackgroundWorker = Substitute.For<IBackgroundWorker>();
        options.AutoSessionTracking = false;
        options.InitNativeSdks = false;
        options.SetBeforeSend((e, _) =>
        {
            _events.Add(e);
            return null;
        });
        options.SetBeforeSendLog(log =>
        {
            _logs.Add(log);
            return null;
        });
    }

    private static void UseGenericSentry<TBuilder>(TBuilder builder, Action<SentryGenericHostOptions>? configureOptions = null)
        where TBuilder : IHostApplicationBuilder
        => builder.UseSentry(configureOptions);

    private WebApplicationBuilder CreateBuilder()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production" });
        builder.WebHost.UseTestServer();
        builder.Logging.AddProvider(_hostLogs);
        return builder;
    }

    private void AssertWarnedOnce()
        => _hostLogs.Entries.Should().ContainSingle(entry => entry.Category == HostedServiceCategory)
            .Which.Should().Match<(string Category, LogLevel Level, string Message)>(entry =>
                entry.Level == LogLevel.Warning && entry.Message.Contains("Sentry.AspNetCore"));

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GenericUseSentry_WithAspNetCoreUseSentry_StepsAside(bool genericFirst)
    {
        var builder = CreateBuilder();
        if (genericFirst)
        {
            UseGenericSentry(builder);
            builder.UseSentry((SentryAspNetCoreOptions options) => DoNotSend(options));
        }
        else
        {
            builder.UseSentry((SentryAspNetCoreOptions options) => DoNotSend(options));
            UseGenericSentry(builder);
        }
        await using var app = builder.Build();
        await app.StartAsync();

        app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("MyApp.Endpoint").LogError("one logged error");

        _events.Should().ContainSingle().Which.Sdk.Name.Should().Be(Constants.SdkName);
        app.Services.GetRequiredService<SentryOptions>().Should().BeOfType<SentryAspNetCoreOptions>();
        _hostLogs.Entries.Should().NotContain(entry => entry.Category == HostedServiceCategory);
        await app.StopAsync();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GenericUseSentryWithAspNetCoreUseSentry_ClearProvidersThenLoggingAddSentry_SendsErrorAndLogOnce(bool genericFirst)
    {
        var builder = CreateBuilder();
        Action<SentryAspNetCoreOptions> configureOptions = options =>
        {
            DoNotSend(options);
            options.EnableLogs = true;
        };
        if (genericFirst)
        {
            UseGenericSentry(builder);
            builder.UseSentry(configureOptions);
        }
        else
        {
            builder.UseSentry(configureOptions);
            UseGenericSentry(builder);
        }
        builder.Logging.ClearProviders();
        builder.Logging.AddSentry();
        await using var app = builder.Build();
        await app.StartAsync();

        app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("MyApp.Endpoint").LogError("one logged error");

        _events.Should().ContainSingle().Which.Sdk.Name.Should().Be(Constants.SdkName);
        _logs.Should().ContainSingle(log => log.Message == "one logged error");
        await app.StopAsync();
    }

    [Fact]
    public async Task GenericUseSentryThenAspNetCoreUseSentry_SdkDisabled_Starts()
    {
        var builder = CreateBuilder();
        UseGenericSentry(builder);
        builder.UseSentry((SentryAspNetCoreOptions options) => options.Dsn = SentryConstants.DisableSdkDsnValue);
        await using var app = builder.Build();

        var start = () => app.StartAsync();

        await start.Should().NotThrowAsync();
        await app.StopAsync();
    }

    [Fact]
    public async Task ServiceDefaultsSnippet_WebApplicationBuilder_UsesAspNetCoreIntegration()
    {
        var builder = CreateBuilder();
        builder.Services.Configure<SentryAspNetCoreOptions>(DoNotSend);
        builder.UseSentryLikeServiceDefaults();
        await using var app = builder.Build();
        await app.StartAsync();

        app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("MyApp.Endpoint").LogError("one logged error");

        _events.Should().ContainSingle().Which.Sdk.Name.Should().Be(Constants.SdkName);
        _hostLogs.Entries.Should().NotContain(entry => entry.Category == HostedServiceCategory);
        await app.StopAsync();
    }

    [Fact]
    public async Task GenericUseSentry_WebApplicationBuilderWithoutAspNetCoreUseSentry_WarnsOnceAtStartup()
    {
        var builder = CreateBuilder();
        UseGenericSentry(builder, options =>
        {
            DoNotSend(options);
            options.EnableLogs = true;
        });
        await using var app = builder.Build();

        await app.StartAsync();

        AssertWarnedOnce();
        _events.Should().BeEmpty();
        _logs.Should().NotContain(log => log.Message.Contains("Sentry.AspNetCore"));
        await app.StopAsync();
    }

    [Fact]
    public async Task GenericUseSentry_WebApplicationHostWithoutAspNetCoreUseSentry_WarnsOnceAtStartup()
    {
        var builder = CreateBuilder();
        builder.Host.UseSentry(options => DoNotSend(options));
        await using var app = builder.Build();

        await app.StartAsync();

        AssertWarnedOnce();
        await app.StopAsync();
    }

    [Fact]
    public async Task GenericUseSentry_HostBuilderHostingAspNetCore_LogsNoWarning()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(web => web.UseTestServer().Configure(_ => { }))
            .ConfigureLogging(logging => logging.AddProvider(_hostLogs))
            .UseSentry(options => DoNotSend(options))
            .Build();

        await host.StartAsync();

        _hostLogs.Entries.Should().NotContain(entry => entry.Category == HostedServiceCategory);
        await host.StopAsync();
    }

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        public ConcurrentQueue<(string Category, LogLevel Level, string Message)> Entries { get; } = new();

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(this, categoryName);

        public void Dispose()
        {
        }

        private sealed class CapturingLogger(CapturingLoggerProvider provider, string categoryName) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter)
                => provider.Entries.Enqueue((categoryName, logLevel, formatter(state, exception)));
        }
    }
}
