using System.Diagnostics;

namespace Sentry.Samples.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder()
            .UseMauiApp<App>()

            // This adds Sentry to your Maui application
            .UseSentry(options =>
            {
                // The DSN is the only required option.
                options.Dsn = "https://eb18e953812b41c3aeb042e666fd3b5c@o447951.ingest.sentry.io/5428537";

                // By default, we will send the last 100 breadcrumbs with each event.
                // If you want to see everything we can capture from MAUI, you may wish to use a larger value.
                options.MaxBreadcrumbs = 1000;

                // Be aware that screenshots may contain PII
                options.AttachScreenshot = true;

                options.Debug = true;
                options.SampleRate = 1.0F;

#if ANDROID
                // Currently experimental support is only available on Android
                options.Native.ExperimentalOptions.SessionReplay.OnErrorSampleRate = 1.0;
                options.Native.ExperimentalOptions.SessionReplay.SessionSampleRate = 1.0;
                options.Native.ExperimentalOptions.SessionReplay.MaskAllImages = false;
                options.Native.ExperimentalOptions.SessionReplay.MaskAllText = false;
#endif

                options.SetBeforeScreenshotCapture((@event, hint) =>
                {
                    Console.WriteLine("screenshot about to be captured.");

                    // Return true to capture or false to prevent the capture
                    return true;
                });
            })
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // For this sample, we'll also register the main page for DI so we can inject a logger there.
        builder.Services.AddTransient<MainPage>();

        return builder.Build();
    }
}
