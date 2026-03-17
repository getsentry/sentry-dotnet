using System.Diagnostics;

namespace Sentry.Samples.MacCatalyst;

[Register("AppDelegate")]
public class AppDelegate : UIApplicationDelegate
{
    public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
    {
        // Override point for customization after application launch.

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
        });

        // Try out the Sentry SDK
        SentrySdk.CaptureMessage("From Mac Catalyst");

        // Uncomment to try these
        // throw new Exception("Test Unhandled Managed Exception");
        // SentrySdk.CauseCrash(CrashType.Native);

        return true;
    }

    public override UISceneConfiguration GetConfiguration(UIApplication application,
        UISceneSession connectingSceneSession, UISceneConnectionOptions options)
    {
        // Called when a new scene session is being created.
        // Use this method to select a configuration to create the new scene with.
        // "Default Configuration" is defined in the Info.plist's 'UISceneConfigurationName' key.
        return new UISceneConfiguration("Default Configuration", connectingSceneSession.Role);
    }

    public override void DidDiscardSceneSessions(UIApplication application, NSSet<UISceneSession> sceneSessions)
    {
        // Called when the user discards a scene session.
        // If any sessions were discarded while the application was not running, this will be called shortly after 'FinishedLaunching'.
        // Use this method to release any resources that were specific to the discarded scenes, as they will not return.
    }
}
