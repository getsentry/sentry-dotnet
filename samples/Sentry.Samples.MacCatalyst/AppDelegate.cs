using System.Diagnostics;

namespace Sentry.Samples.MacCatalyst;

[Register("AppDelegate")]
public class AppDelegate : UIApplicationDelegate
{
    public override UIWindow? Window
    {
        get;
        set;
    }

    public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
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
        });

        // create a new window instance based on the screen size
        Window = new UIWindow(UIScreen.MainScreen.Bounds);

        // create a UIViewController with a single UILabel
        var vc = new UIViewController();
        vc.View!.AddSubview(new UILabel(Window!.Frame)
        {
            BackgroundColor = UIColor.SystemBackground,
            TextAlignment = UITextAlignment.Center,
            Text = "Hello, Catalyst!",
            AutoresizingMask = UIViewAutoresizing.All,
        });
        Window.RootViewController = vc;

        // make the window visible
        Window.MakeKeyAndVisible();

        // Try out the Sentry SDK
        SentrySdk.CaptureMessage("From Mac Catalyst");

        // Uncomment to try these
        // throw new Exception("Test Unhandled Managed Exception");
        // SentrySdk.CauseCrash(CrashType.Native);

        return true;
    }
}
