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
#if ANDROID
                options.Dsn = "{{SENTRY_DSN}}";
#endif
                options.Debug = true;
                options.DiagnosticLevel = SentryLevel.Info;
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
