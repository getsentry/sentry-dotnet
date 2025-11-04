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

                options.SetBeforeBreadcrumb((breadcrumb, hint) =>
                {
                    if (breadcrumb.Data?.TryGetValue("action", out string action) == true && App.HasTestArg(action))
                    {
                        SentrySdk.CaptureMessage(action, scope =>
                        {
                            scope.SetExtra("action", action);
                            scope.SetExtra("category", breadcrumb.Category);
                            scope.SetExtra("thread_id", Thread.CurrentThread.ManagedThreadId);
                            scope.SetExtra("type", breadcrumb.Type);
                        });
                        App.Kill();
                    }
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
