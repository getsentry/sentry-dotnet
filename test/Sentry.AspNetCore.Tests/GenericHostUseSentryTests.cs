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
    private readonly List<SentryEvent> _events = new();

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
    }

    private static void UseGenericSentry<TBuilder>(TBuilder builder) where TBuilder : IHostApplicationBuilder
        => builder.UseSentry();

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GenericUseSentry_WithAspNetCoreUseSentry_StepsAside(bool genericFirst)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production" });
        builder.WebHost.UseTestServer();
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
        await app.StopAsync();
    }
}
