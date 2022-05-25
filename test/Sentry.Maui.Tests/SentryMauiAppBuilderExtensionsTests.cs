using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Maui.Hosting;

namespace Sentry.Maui.Tests;

public class SentryMauiAppBuilderExtensionsTests
{
    [Fact]
    public void CanUseSentry_WithConfigurationOnly()
    {
        // Arrange
        var builder = MauiApp.CreateBuilder();
        builder.Services.Configure<SentryMauiOptions>(options =>
        {
            options.Dsn = DsnSamples.ValidDsnWithoutSecret;
        });

        // Act
        var chainedBuilder = builder.UseSentry();

        using var app = builder.Build();
        var options = app.Services.GetRequiredService<IOptions<SentryMauiOptions>>().Value;

        // Assert
        Assert.Same(builder, chainedBuilder);
        Assert.True(SentrySdk.IsEnabled);
        Assert.Equal(DsnSamples.ValidDsnWithoutSecret, options.Dsn);
        Assert.False(options.Debug);
    }

    [Fact]
    public void CanUseSentry_WithDsnStringOnly()
    {
        // Arrange
        var builder = MauiApp.CreateBuilder();

        // Act
        var chainedBuilder = builder.UseSentry(DsnSamples.ValidDsnWithoutSecret);

        using var app = builder.Build();
        var options = app.Services.GetRequiredService<IOptions<SentryMauiOptions>>().Value;

        // Assert
        Assert.Same(builder, chainedBuilder);
        Assert.True(SentrySdk.IsEnabled);
        Assert.Equal(DsnSamples.ValidDsnWithoutSecret, options.Dsn);
        Assert.False(options.Debug);
    }

    [Fact]
    public void CanUseSentry_WithOptionsOnly()
    {
        // Arrange
        var builder = MauiApp.CreateBuilder();

        // Act
        var chainedBuilder = builder.UseSentry(options =>
        {
            options.Dsn = DsnSamples.ValidDsnWithoutSecret;
        });

        using var app = builder.Build();
        var options = app.Services.GetRequiredService<IOptions<SentryMauiOptions>>().Value;

        // Assert
        Assert.Same(builder, chainedBuilder);
        Assert.True(SentrySdk.IsEnabled);
        Assert.Equal(DsnSamples.ValidDsnWithoutSecret, options.Dsn);
        Assert.False(options.Debug);
    }

    [Fact]
    public void CanUseSentry_WithConfigurationAndOptions()
    {
        // Arrange
        var builder = MauiApp.CreateBuilder();
        builder.Services.Configure<SentryMauiOptions>(options =>
        {
            options.Dsn = DsnSamples.ValidDsnWithoutSecret;
        });

        // Act
        var chainedBuilder = builder.UseSentry(options =>
        {
            options.Debug = true;
        });

        using var app = builder.Build();
        var options = app.Services.GetRequiredService<IOptions<SentryMauiOptions>>().Value;

        // Assert
        Assert.Same(builder, chainedBuilder);
        Assert.True(SentrySdk.IsEnabled);
        Assert.Equal(DsnSamples.ValidDsnWithoutSecret, options.Dsn);
        Assert.True(options.Debug);
    }

    [Fact]
    public void UseSentry_EnablesGlobalMode()
    {
        // Arrange
        var builder = MauiApp.CreateBuilder();

        // Act
        _ = builder.UseSentry(DsnSamples.ValidDsnWithoutSecret);

        using var app = builder.Build();
        var options = app.Services.GetRequiredService<IOptions<SentryMauiOptions>>().Value;

        // Assert
        Assert.True(options.IsGlobalModeEnabled);
    }

    [Fact]
    public void UseSentry_SetsMauiSdkNameAndVersion()
    {
        // Arrange
        SentryEvent @event = null;
        var builder = MauiApp.CreateBuilder()
            .UseSentry(options =>
            {
                options.Dsn = DsnSamples.ValidDsnWithoutSecret;
                options.BeforeSend = e =>
                {
                    // capture the event
                    @event = e;

                    // but don't actually send it
                    return null;
                };
            });

        // Act
        using var app = builder.Build();
        var client = app.Services.GetRequiredService<ISentryClient>();
        client.CaptureMessage("test");

        // Assert
        Assert.NotNull(@event);
        Assert.Equal(Constants.SdkName, @event.Sdk.Name);
        Assert.Equal(Constants.SdkVersion, @event.Sdk.Version);
    }
}
