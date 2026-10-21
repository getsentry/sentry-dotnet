using ObjCRuntime;

namespace Sentry.Samples.Ios;

[Register("AppDelegate")]
public class AppDelegate : UIApplicationDelegate
{
    public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
    {
        // Init the Sentry SDK
        SentrySdk.Init(options =>
        {
#if !SENTRY_DSN_DEFINED_IN_ENV
            // You must specify a DSN. On mobile platforms, this should be done in code here.
            // See https://docs.sentry.io/product/sentry-basics/dsn-explainer/
            options.Dsn = SamplesShared.Dsn;
#else
            // To make things easier for the SDK maintainers our samples check for a SENTRY_DSN environment variable
            // and write this (as a constant) into an EnvironmentVariables class. Generally, you won't want to do
            // this in your own mobile projects though - you should set the DSN in code as above
            options.Dsn = EnvironmentVariables.Dsn;
#endif
            options.Debug = true;
            options.SampleRate = 1.0F;
            options.TracesSampleRate = 1.0;
            options.ProfilesSampleRate = 1.0;

            // All the native iOS SDK options are available below
            // https://docs.sentry.io/platforms/apple/guides/ios/configuration/
            // Enable Native iOS SDK App Hangs detection
            options.Native.EnableAppHangTracking = true;

            // If your app doesn't have sensitive data, you can get screenshots on error events automatically
            // https://docs.sentry.io/platforms/apple/guides/ios/configuration/options/#attachScreenshot
            options.Native.AttachScreenshot = true;

            options.CacheDirectoryPath = Path.GetTempPath();

            options.SetBeforeSend(evt =>
            {
                if (evt.Exception?.Message.Contains("Something you don't care want logged?") ?? false)
                {
                    return null; // return null to filter out event
                }
                // or add additional data
                evt.SetTag("dotnet-iOS-Native-Before", "Hello World");
                return evt;
            });

            options.OnCrashedLastRun = e =>
            {
                Console.WriteLine(e);
            };
        });

        return true;
    }
}
