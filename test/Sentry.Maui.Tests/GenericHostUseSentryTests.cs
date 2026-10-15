using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MauiConstants = Sentry.Maui.Internal.Constants;

namespace Sentry.Maui.Tests;

[Collection(nameof(SentrySdkCollection))]
public class GenericHostUseSentryTests : IDisposable
{
    private readonly List<SentryEvent> _events = new();

    public void Dispose() => SentrySdk.Close();

    private void DoNotSend(SentryMauiOptions options)
    {
        options.Dsn = ValidDsn;
        options.Transport = Substitute.For<ITransport>();
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
    public void GenericUseSentry_WithMauiUseSentry_StepsAside(bool genericFirst)
    {
        var builder = MauiApp.CreateBuilder();
        builder.Services.AddSingleton(Substitute.For<IApplication>());
        if (genericFirst)
        {
            UseGenericSentry(builder);
            builder.UseSentry(DoNotSend);
        }
        else
        {
            builder.UseSentry(DoNotSend);
            UseGenericSentry(builder);
        }

        using var app = builder.Build();
        app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("MyApp.Page").LogError("one logged error");

        _events.Should().ContainSingle().Which.Sdk.Name.Should().Be(MauiConstants.SdkName);
        app.Services.GetRequiredService<SentryOptions>().Should().BeOfType<SentryMauiOptions>();
    }
}
