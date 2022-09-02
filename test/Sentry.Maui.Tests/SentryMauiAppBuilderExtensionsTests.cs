using Microsoft.Extensions.Options;
using MauiConstants = Sentry.Maui.Internal.Constants;

namespace Sentry.Maui.Tests;

public class SentryMauiAppBuilderExtensionsTests
{
    private class Fixture
    {
        public MauiAppBuilder Builder { get; }

        public Fixture()
        {
            var builder = MauiApp.CreateBuilder();
            builder.Services.AddSingleton(Substitute.For<IApplication>());
            Builder = builder;
        }
    }

    private readonly Fixture _fixture = new();

    [Fact]
    public void CanUseSentry_WithConfigurationOnly()
    {
        // Arrange
        var builder = _fixture.Builder;
        builder.Services.Configure<SentryMauiOptions>(options =>
        {
            options.Dsn = ValidDsn;
        });

        // Act
        var chainedBuilder = builder.UseSentry();

        using var app = builder.Build();
        var options = app.Services.GetRequiredService<IOptions<SentryMauiOptions>>().Value;

        // Assert
        Assert.Same(builder, chainedBuilder);
        Assert.True(SentrySdk.IsEnabled);
        Assert.Equal(ValidDsn, options.Dsn);
        Assert.False(options.Debug);
    }

    [Fact]
    public void CanUseSentry_WithDsnStringOnly()
    {
        // Arrange
        var builder = _fixture.Builder;

        // Act
        var chainedBuilder = builder.UseSentry(ValidDsn);

        using var app = builder.Build();
        var options = app.Services.GetRequiredService<IOptions<SentryMauiOptions>>().Value;

        // Assert
        Assert.Same(builder, chainedBuilder);
        Assert.True(SentrySdk.IsEnabled);
        Assert.Equal(ValidDsn, options.Dsn);
        Assert.False(options.Debug);
    }

    [Fact]
    public void CanUseSentry_WithOptionsOnly()
    {
        // Arrange
        var builder = _fixture.Builder;

        // Act
        var chainedBuilder = builder.UseSentry(options =>
        {
            options.Dsn = ValidDsn;
        });

        using var app = builder.Build();
        var options = app.Services.GetRequiredService<IOptions<SentryMauiOptions>>().Value;

        // Assert
        Assert.Same(builder, chainedBuilder);
        Assert.True(SentrySdk.IsEnabled);
        Assert.Equal(ValidDsn, options.Dsn);
        Assert.False(options.Debug);
    }

    [Fact]
    public void CanUseSentry_WithConfigurationAndOptions()
    {
        // Arrange
        var builder = _fixture.Builder;
        builder.Services.Configure<SentryMauiOptions>(options =>
        {
            options.Dsn = ValidDsn;
        });

        // Act
        var chainedBuilder = builder.UseSentry(options =>
        {
            options.Release = "test";
        });

        using var app = builder.Build();
        var options = app.Services.GetRequiredService<IOptions<SentryMauiOptions>>().Value;

        // Assert
        Assert.Same(builder, chainedBuilder);
        Assert.True(SentrySdk.IsEnabled);
        Assert.Equal(ValidDsn, options.Dsn);
        Assert.Equal("test", options.Release);
    }

    [Fact]
    public void UseSentry_EnablesGlobalMode()
    {
        // Arrange
        var builder = _fixture.Builder;

        // Act
        _ = builder.UseSentry(ValidDsn);

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
        var builder = _fixture.Builder
            .UseSentry(options =>
            {
                options.Dsn = ValidDsn;
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
        Assert.Equal(MauiConstants.SdkName, @event.Sdk.Name);
        Assert.Equal(MauiConstants.SdkVersion, @event.Sdk.Version);
    }

    [Fact]
    public void UseSentry_EnablesHub()
    {
        // Arrange
        var builder = _fixture.Builder
            .UseSentry(ValidDsn);

        // Act
        using var app = builder.Build();
        var hub = app.Services.GetRequiredService<IHub>();

        // Assert
        Assert.True(hub.IsEnabled);
    }

    [Fact]
    public void UseSentry_AppDispose_DisposesHub()
    {
        // Note: It's crucial to dispose the hub when the app disposes, so that we flush events from the
        //       queue in the BackgroundWorker.  The app container will dispose any services registered
        //       that implement IDisposable.

        // Arrange
        var builder = _fixture.Builder
            .UseSentry(ValidDsn);

        // Act
        IHub hub;
        using (var app = builder.Build())
        {
            hub = app.Services.GetRequiredService<IHub>();
        }

        // Assert
        // Note, the hub is disabled when disposed.  We ensure it's first enabled in the previous test.
        Assert.False(hub.IsEnabled);
    }
}
