using System.Diagnostics;
using Sentry.Maui;

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
                options.Experimental.EnableLogs = true;
                options.SampleRate = 1.0F;

                // The Sentry MVVM Community Toolkit integration automatically creates traces for async relay commands,
                // but only if tracing is enabled. Here we capture all traces (in a production app you'd probably only
                // capture a certain percentage)
                options.TracesSampleRate = 1.0F;

                // Automatically create traces for async relay commands in the MVVM Community Toolkit
                options.AddCommunityToolkitIntegration();

#if __ANDROID__
                // Currently experimental support is only available on Android
                options.Native.ExperimentalOptions.SessionReplay.OnErrorSampleRate = 1.0;
                options.Native.ExperimentalOptions.SessionReplay.SessionSampleRate = 1.0;
                // Mask all images and text by default. This can be overridden for individual view elements via the
                // sentry:SessionReplay.Mask XML attribute (see MainPage.xaml for an example)
                options.Native.ExperimentalOptions.SessionReplay.MaskAllImages = true;
                options.Native.ExperimentalOptions.SessionReplay.MaskAllText = true;
                // Alternatively the masking behaviour for entire classes of VisualElements can be configured here as
                // an exception to the default behaviour.
                options.Native.ExperimentalOptions.SessionReplay.UnmaskControlsOfType<Button>();
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
