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
                options.Native.SignalHandlerStrategy = Sentry.Android.SignalHandlerStrategy.ChainAtStart;
#endif
                options.Debug = false;
                options.DiagnosticLevel = SentryLevel.Error;
                // predictable crash envelopes only
                options.SendClientReports = false;
                options.AutoSessionTracking = false;

                options.SetBeforeBreadcrumb((breadcrumb, hint) =>
                {
                    App.ReceiveSystemBreadcrumb(breadcrumb);
                    return breadcrumb;
                });
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
