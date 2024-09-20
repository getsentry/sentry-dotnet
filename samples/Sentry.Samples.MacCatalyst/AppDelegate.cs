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
            options.Dsn = "https://eb18e953812b41c3aeb042e666fd3b5c@o447951.ingest.sentry.io/5428537";
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
