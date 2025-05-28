using Microsoft.Extensions.Options;
using Sentry.Internal.Http;
using Sentry.Maui.Internal;
using MauiConstants = Sentry.Maui.Internal.Constants;

namespace Sentry.Maui.Tests;

public partial class SentryMauiAppBuilderExtensionsTests
{
    private class Fixture
    {
        public MauiAppBuilder Builder { get; }

        public Fixture()
        {
            var builder = MauiApp.CreateBuilder();
            builder.Services.AddSingleton(Substitute.For<IApplication>());

            builder.Services.Configure<SentryMauiOptions>(options =>
            {
                options.Transport = Substitute.For<ITransport>();
                options.Dsn = ValidDsn;
                options.AutoSessionTracking = false;
                options.InitNativeSdks = false;
            });

            Builder = builder;
        }
    }

    private readonly Fixture _fixture = new();

    [Fact]
    public void UseSentry_RegistersEventProcessorOnlyOnce()
    {
        // Arrange
        var builder = _fixture.Builder;
        builder.Services.Configure<SentryMauiOptions>(options =>
        {
            options.Dsn = ValidDsn;
        });

        // Act
        using var app = builder.UseSentry().Build();

        // Assert
        var options = app.Services.GetRequiredService<IOptions<SentryMauiOptions>>().Value;
        options.EventProcessors.Should().ContainSingle(t => t.Type == typeof(SentryMauiEventProcessor));
    }

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
        var hub = app.Services.GetRequiredService<IHub>();

        // Assert
        Assert.Same(builder, chainedBuilder);
        Assert.True(hub.IsEnabled);
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
        var hub = app.Services.GetRequiredService<IHub>();

        // Assert
        Assert.Same(builder, chainedBuilder);
        Assert.True(hub.IsEnabled);
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
        var hub = app.Services.GetRequiredService<IHub>();

        // Assert
        Assert.Same(builder, chainedBuilder);
        Assert.True(hub.IsEnabled);
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
        var hub = app.Services.GetRequiredService<IHub>();

        // Assert
        Assert.Same(builder, chainedBuilder);
        Assert.True(hub.IsEnabled);
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
                options.SetBeforeSend((e, _) =>
                    {
                        // capture the event
                        @event = e;

                        // but don't actually send it
                        return null;
                    }
                );
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
        var builder = _fixture.Builder.UseSentry(ValidDsn);

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
        var builder = _fixture.Builder.UseSentry(ValidDsn);

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

    [Fact]
    public void UseSentry_WithCaching_Default()
    {
        // Arrange
        var builder = _fixture.Builder;

        // Act
        builder.UseSentry();

        using var app = builder.Build();
        var options = app.Services.GetRequiredService<IOptions<SentryMauiOptions>>().Value;

        // Assert
#if PLATFORM_NEUTRAL
        Assert.Null(options.CacheDirectoryPath);
        Assert.IsNotType<CachingTransport>(options.Transport);
#else
        var expectedPath = Microsoft.Maui.Storage.FileSystem.CacheDirectory;
        Assert.Equal(expectedPath, options.CacheDirectoryPath);
        Assert.IsType<CachingTransport>(options.Transport);
#endif
    }

    [Fact]
    public void UseSentry_WithCaching_CanChangeCacheDirectoryPath()
    {
        // Arrange
        var builder = _fixture.Builder;
        using var cacheDirectory = new TempDirectory();
        var cachePath = cacheDirectory.Path;

        // Act
        builder.UseSentry(options =>
        {
            options.CacheDirectoryPath = cachePath;
        });

        using var app = builder.Build();
        var options = app.Services.GetRequiredService<IOptions<SentryMauiOptions>>().Value;

        // Assert
        Assert.Equal(cachePath, options.CacheDirectoryPath);
    }

    [Fact]
    public void UseSentry_DebugFalse_LoggerLeftDefault()
    {
        // Arrange
        var builder = _fixture.Builder;

        // Act
        builder.UseSentry(options =>
        {
            options.Debug = false;
            options.Dsn = ValidDsn;
        });

        using var app = builder.Build();
        var options = app.Services.GetRequiredService<IOptions<SentryMauiOptions>>().Value;

        // Assert
        options.DiagnosticLogger.Should().BeNull();
    }

    [Fact]
    public void UseSentry_DebugTrue_ConsoleAndTracingDiagnosticsLogger()
    {
        // Arrange
        var builder = _fixture.Builder;

        // Act
        builder.UseSentry(options =>
        {
            options.Debug = true;
            options.Dsn = ValidDsn;
        });

        using var app = builder.Build();
        var options = app.Services.GetRequiredService<IOptions<SentryMauiOptions>>().Value;

        // Assert
        options.DiagnosticLogger.Should().BeOfType<ConsoleAndTraceDiagnosticLogger>();
    }

    [Fact]
    public void UseSentry_DebugTrue_CustomDiagnosticsLogger()
    {
        // Arrange
        var builder = _fixture.Builder;

        // Act
        builder.UseSentry(options =>
        {
            options.Debug = true;
            options.Dsn = ValidDsn;
            options.DiagnosticLogger = new TraceDiagnosticLogger(SentryLevel.Fatal);
        });

        using var app = builder.Build();
        var options = app.Services.GetRequiredService<IOptions<SentryMauiOptions>>().Value;

        // Assert
        options.DiagnosticLogger.Should().BeOfType<TraceDiagnosticLogger>();
    }
}
