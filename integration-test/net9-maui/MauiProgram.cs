using Microsoft.Extensions.Logging;
using Sentry.Maui;

namespace Sentry.Maui.Device.IntegrationTestApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseSentry(options =>
            {
                options.Dsn = System.Environment.GetEnvironmentVariable("SENTRY_DSN") ?? "http://key@127.0.0.1:8000/0";
                options.Debug = false;
                options.DiagnosticLevel = SentryLevel.Error;
                // predictable crash envelopes only
                options.SendClientReports = false;
                options.AutoSessionTracking = false;
            })
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
