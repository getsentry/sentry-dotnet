using Microsoft.Maui.Hosting;

namespace Sentry.Maui.Tests;

public class SentryMauiAppBuilderExtensionsTests
{
    [Fact]
    public void CanUseSentry()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseSentry();
    }
}
