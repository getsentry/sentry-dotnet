using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Hosting;

namespace Sentry.Maui.Tests;

public class SentryMauiAppBuilderExtensionsTests
{
    [Fact]
    public void CanUseSentry()
    {
        // Arrange
        var builder = MauiApp.CreateBuilder();
        builder.Services.Configure<SentryMauiOptions>(o => o.Dsn = DsnSamples.ValidDsnWithSecret);

        // Act
        var chainedBuilder = builder.UseSentry();
        using var app = builder.Build();

        // Assert
        Assert.Same(builder, chainedBuilder);
        Assert.True(SentrySdk.IsEnabled);
    }
}
