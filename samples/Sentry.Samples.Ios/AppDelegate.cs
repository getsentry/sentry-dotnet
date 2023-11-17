namespace Sentry.Samples.Ios;

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
        SentrySdk.Init(o =>
        {
            o.Debug = true;
            o.Dsn = "https://eb18e953812b41c3aeb042e666fd3b5c@o447951.ingest.sentry.io/5428537";
        });

        // create a new window instance based on the screen size
        Window = new UIWindow(UIScreen.MainScreen.Bounds);

        // determine the background color for the view (SystemBackground requires iOS >= 13.0)
        var backgroundColor = UIDevice.CurrentDevice.CheckSystemVersion(13, 0)
#pragma warning disable CA1416
            ? UIColor.SystemBackground
#pragma warning restore CA1416
            : UIColor.White;

        // create a UIViewController with a single UILabel
        var vc = new UIViewController();
        vc.View!.AddSubview(new UILabel(Window!.Frame)
        {
            BackgroundColor = backgroundColor,
            TextAlignment = UITextAlignment.Center,
            Text = "Hello, iOS!",
            AutoresizingMask = UIViewAutoresizing.All,
        });
        Window.RootViewController = vc;

        // make the window visible
        Window.MakeKeyAndVisible();


        // Try out the Sentry SDK
        SentrySdk.CaptureMessage("From iOS");

        // Uncomment to try these
        // throw new Exception("Test Unhandled Managed Exception");
        // SentrySdk.CauseCrash(CrashType.Native);

        return true;
    }
}
