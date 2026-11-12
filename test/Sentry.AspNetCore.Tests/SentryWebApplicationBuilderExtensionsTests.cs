using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sentry.Extensions.Logging;

namespace Sentry.AspNetCore.Tests;

[Collection(nameof(SentrySdkCollection))]
public class SentryWebApplicationBuilderExtensionsTests : IDisposable
{
    public void Dispose() => SentrySdk.Close();

    private static WebApplicationBuilder CreateBuilder(
        string environmentName = "Production",
        params (string Key, string Value)[] settings)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = environmentName });
        ((IConfigurationBuilder)builder.Configuration).AddInMemoryCollection(
            settings.ToDictionary(s => s.Key, s => s.Value));
        return builder;
    }

    private static SentryAspNetCoreOptions ResolveOptions(WebApplicationBuilder builder)
    {
        using var services = builder.Services.BuildServiceProvider();
        return services.GetRequiredService<IOptions<SentryAspNetCoreOptions>>().Value;
    }

    private static void DoNotSend(SentryAspNetCoreOptions options)
    {
        options.Dsn = ValidDsn;
        options.BackgroundWorker = Substitute.For<IBackgroundWorker>();
        options.AutoSessionTracking = false;
        options.InitNativeSdks = false;
    }

    public static TheoryData<string, Func<WebApplicationBuilder, WebApplicationBuilder>> Overloads => new()
    {
        { "parameterless", b => b.UseSentry() },
        { "dsn", b => b.UseSentry(ValidDsn) },
        { "options callback", b => b.UseSentry((SentryAspNetCoreOptions o) => DoNotSend(o)) },
        { "sentry builder callback", b => b.UseSentry((ISentryBuilder s) => s.AddSentryOptions(DoNotSend)) },
    };

    [Theory, MemberData(nameof(Overloads))]
    public void UseSentry_AnyOverload_RegistersAspNetCoreIntegration(
        string overload, Func<WebApplicationBuilder, WebApplicationBuilder> useSentry)
    {
        _ = overload;
        var builder = CreateBuilder();

        var returned = useSentry(builder);

        returned.Should().BeSameAs(builder);
        builder.Services.Should().ContainSingle(d => d.ServiceType == typeof(IHub));
        builder.Services.Should().ContainSingle(d => d.ImplementationType == typeof(SentryStartupFilter));
        builder.Services.Should().ContainSingle(d => d.ImplementationType == typeof(SentryTracingStartupFilter));
        builder.Services.Should().ContainSingle(d => d.ImplementationType == typeof(SentryMiddleware));
        builder.Services.Should().ContainSingle(d => d.ServiceType == typeof(HostLoggerProviders));
    }

    [Fact]
    public void UseSentry_SentryAndLoggingSections_BindsOptions()
    {
        var builder = CreateBuilder(settings: new[]
        {
            ("Sentry:Dsn", ValidDsn),
            ("Sentry:SendDefaultPii", "true"),
            ("Logging:Sentry:MinimumEventLevel", "Warning"),
        });

        builder.UseSentry();

        var options = ResolveOptions(builder);
        options.Dsn.Should().Be(ValidDsn);
        options.SendDefaultPii.Should().BeTrue();
        options.MinimumEventLevel.Should().Be(LogLevel.Warning);
    }

    [Fact]
    public void UseSentry_Dsn_SetsDsn()
    {
        var builder = CreateBuilder();

        builder.UseSentry(ValidDsn);

        ResolveOptions(builder).Dsn.Should().Be(ValidDsn);
    }

    [Fact]
    public void UseSentry_SentryBuilderCallback_ConfiguresOptions()
    {
        var builder = CreateBuilder();

        builder.UseSentry((ISentryBuilder sentryBuilder) =>
            sentryBuilder.AddSentryOptions(options => options.SendDefaultPii = true));

        ResolveOptions(builder).SendDefaultPii.Should().BeTrue();
    }

    [Fact]
    public void UseSentry_OptionsCallback_RunsAfterConfigurationBinding()
    {
        var builder = CreateBuilder(settings: ("Sentry:SendDefaultPii", "true"));

        builder.UseSentry(options => options.SendDefaultPii = false);

        ResolveOptions(builder).SendDefaultPii.Should().BeFalse();
    }

    [Fact]
    public void UseSentry_NoEnvironmentConfigured_UsesHostEnvironment()
    {
        var builder = CreateBuilder("Staging");

        builder.UseSentry();

        ResolveOptions(builder).Environment.Should().Be("staging");
    }

    [Fact]
    public void UseSentry_AdjustStandardEnvironmentNameCasingInConfiguration_KeepsHostEnvironmentCasing()
    {
        var builder = CreateBuilder("Staging", ("Sentry:AdjustStandardEnvironmentNameCasing", "false"));

        builder.UseSentry();

        ResolveOptions(builder).Environment.Should().Be("Staging");
    }

    [Fact]
    public void UseSentry_EnvironmentSetInCallback_KeepsEnvironment()
    {
        var builder = CreateBuilder("Staging");

        builder.UseSentry(options => options.Environment = "from-code");

        ResolveOptions(builder).Environment.Should().Be("from-code");
    }

    [Fact]
    public async Task UseSentry_UnhandledRequestException_CapturedBySentryMiddleware()
    {
        var events = new List<SentryEvent>();
        var builder = CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.UseSentry(options =>
        {
            DoNotSend(options);
            options.SetBeforeSend((e, _) =>
            {
                events.Add(e);
                return null;
            });
        });
        await using var app = builder.Build();
        app.MapGet("/throw", string () => throw new InvalidOperationException("thrown from an endpoint"));
        await app.StartAsync();

        _ = await Record.ExceptionAsync(() => app.GetTestClient().GetAsync("/throw"));

        events.Should().ContainSingle(e => e.Exception!.Message == "thrown from an endpoint");
    }
}
