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
                options.Debug = false;
                options.DiagnosticLevel = SentryLevel.Error;
                // predictable crash envelopes only
                options.SendClientReports = false;
                options.AutoSessionTracking = false;

                // Only check breadcrumbs for non-crash tests...
                var testArg = System.Environment.GetEnvironmentVariable("SENTRY_TEST_ARG");
                if (!Enum.TryParse<CrashType>(testArg, ignoreCase: true, out var crashType))
                {
                    options.SetBeforeBreadcrumb((breadcrumb, hint) =>
                    {
                        App.ReceiveSystemBreadcrumb(breadcrumb);
                        return breadcrumb;
                    });
                }
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
