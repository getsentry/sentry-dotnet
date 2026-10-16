using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sentry.Extensions.Hosting;
using MauiConstants = Sentry.Maui.Internal.Constants;

namespace Sentry.Maui.Tests;

[Collection(nameof(SentrySdkCollection))]
public class GenericHostUseSentryTests : IDisposable
{
    private const string MissingMauiIntegration = "Sentry: this is a MAUI app";

    private readonly List<SentryEvent> _events = new();

    public void Dispose() => SentrySdk.Close();

    private void DoNotSend(SentryOptions options)
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

    private void UseGenericSentry<TBuilder>(TBuilder builder) where TBuilder : IHostApplicationBuilder
        => builder.UseSentry((SentryGenericHostOptions options) => DoNotSend(options));

    private static MauiAppBuilder CreateBuilder()
    {
        var builder = MauiApp.CreateBuilder();
        builder.Services.AddSingleton(Substitute.For<IApplication>());
        return builder;
    }

    private static void LogError(MauiApp app)
        => app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("MyApp.Page").LogError("one logged error");

    private static string CaptureStandardError(Action action)
    {
        var original = Console.Error;
        using var standardError = new StringWriter();
        Console.SetError(standardError);
        try
        {
            action();
        }
        finally
        {
            Console.SetError(original);
        }

        return standardError.ToString();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GenericUseSentry_WithMauiUseSentry_StepsAside(bool genericFirst)
    {
        var standardError = CaptureStandardError(() =>
        {
            var builder = CreateBuilder();
            if (genericFirst)
            {
                UseGenericSentry(builder);
                builder.UseSentry((SentryMauiOptions options) => DoNotSend(options));
            }
            else
            {
                builder.UseSentry((SentryMauiOptions options) => DoNotSend(options));
                UseGenericSentry(builder);
            }

            using var app = builder.Build();
            LogError(app);
            app.Services.GetRequiredService<SentryOptions>().Should().BeOfType<SentryMauiOptions>();
        });

        _events.Should().ContainSingle().Which.Sdk.Name.Should().Be(MauiConstants.SdkName);
        standardError.Should().NotContain(MissingMauiIntegration);
    }

    [Fact]
    public void ServiceDefaultsSnippet_MauiAppBuilder_UsesMauiIntegration()
    {
        var standardError = CaptureStandardError(() =>
        {
            var builder = CreateBuilder();
            builder.Services.Configure<SentryMauiOptions>(DoNotSend);
            builder.UseSentryLikeServiceDefaults();
            using var app = builder.Build();
            LogError(app);
        });

        _events.Should().ContainSingle().Which.Sdk.Name.Should().Be(MauiConstants.SdkName);
        standardError.Should().NotContain(MissingMauiIntegration);
    }

    [Fact]
    public void GenericUseSentry_WithoutMauiUseSentry_WarnsOnceOnStandardError()
    {
        var standardError = CaptureStandardError(() =>
        {
            var builder = CreateBuilder();
            UseGenericSentry(builder);
            using var app = builder.Build();
            LogError(app);
            LogError(app);
        });

        standardError.Split(MissingMauiIntegration).Should().HaveCount(2);
        standardError.Should().Contain("Sentry.Maui");
        _events.Should().HaveCount(2);
    }

    [Fact]
    public void GenericUseSentry_WithoutMauiUseSentry_HostedServicesCanBeResolved()
    {
        var builder = CreateBuilder();
        UseGenericSentry(builder);
        using var app = builder.Build();

        var resolve = () => app.Services.GetServices<IHostedService>().ToList();

        resolve.Should().NotThrow();
    }
}
