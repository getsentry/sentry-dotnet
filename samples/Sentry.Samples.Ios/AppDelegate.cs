using ObjCRuntime;

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
        SentrySdk.Init(options =>
        {
            options.Dsn = "https://eb18e953812b41c3aeb042e666fd3b5c@o447951.ingest.sentry.io/5428537";
            options.Debug = true;
            options.TracesSampleRate = 1.0;
            options.ProfilesSampleRate = 1.0;

            // All the native iOS SDK options are available below
            // https://docs.sentry.io/platforms/apple/guides/ios/configuration/
            // Enable Native iOS SDK App Hangs detection
            options.Native.EnableAppHangTracking = true;

            options.CacheDirectoryPath = Path.Combine(Path.GetTempPath(), "test12");
        });

        // create a new window instance based on the screen size
        Window = new UIWindow(UIScreen.MainScreen.Bounds);

        // determine control colours (SystemBackground requires iOS >= 13.0)
        var backgroundColor = UIDevice.CurrentDevice.CheckSystemVersion(13, 0)
#pragma warning disable CA1416
            ? UIColor.SystemBackground
#pragma warning restore CA1416
            : UIColor.White;
        var buttonConfig = UIButtonConfiguration.TintedButtonConfiguration;

        var vc = new UIViewController();

        var label = new UILabel
        {
            BackgroundColor = backgroundColor,
            TextAlignment = UITextAlignment.Center,
            Text = "Hello, iOS!",
            AutoresizingMask = UIViewAutoresizing.All
        };

        // UIButton for managed crash
        var managedCrashButton = new UIButton(UIButtonType.RoundedRect)
        {
            AutoresizingMask = UIViewAutoresizing.All,
            Configuration = buttonConfig
        };
        managedCrashButton.SetTitle("Managed Crash", UIControlState.Normal);
        managedCrashButton.TouchUpInside += delegate
        {
            Console.WriteLine("Managed Crash button clicked!");
            try
            {
                string s = null!;
                Console.WriteLine("Length: {0}", s.Length);
            }
            catch (Exception e)
            {
                SentrySdk.CaptureException(e);
            }
        };

        // UIButton for native crash
        var nativeCrashButton = new UIButton(UIButtonType.System)
        {
            Configuration = buttonConfig
        };
        nativeCrashButton.SetTitle("Native Crash", UIControlState.Normal);
        nativeCrashButton.TouchUpInside += delegate
        {
            Console.WriteLine("Native Crash button clicked!");
#pragma warning disable CS0618 // Type or member is obsolete
            SentrySdk.CauseCrash(CrashType.Native);
#pragma warning restore CS0618 // Type or member is obsolete
        };

        // create a UIStackView to hold the label and buttons
        var stackView = new UIStackView(new UIView[] { label, managedCrashButton, nativeCrashButton })
        {
            Axis = UILayoutConstraintAxis.Vertical,
            Distribution = UIStackViewDistribution.FillEqually,
            Alignment = UIStackViewAlignment.Center,
            Spacing = 10,
            TranslatesAutoresizingMaskIntoConstraints = false,
        };

        // add the stack view to the view controller's view
        vc.View!.BackgroundColor = backgroundColor;
        vc.View.AddSubview(stackView);

        // set constraints for the stack view
        NSLayoutConstraint.ActivateConstraints([
            stackView.CenterXAnchor.ConstraintEqualTo(vc.View.CenterXAnchor),
            stackView.CenterYAnchor.ConstraintEqualTo(vc.View.CenterYAnchor),
            stackView.WidthAnchor.ConstraintEqualTo(vc.View.WidthAnchor, 0.8f),
            stackView.HeightAnchor.ConstraintEqualTo(vc.View.HeightAnchor, 0.5f)
        ]);

        Window.RootViewController = vc;

        // make the window visible
        Window.MakeKeyAndVisible();

        AppDomain.CurrentDomain.UnhandledException += (_, _) =>
        {
            Console.WriteLine("In UnhandledException Handler");
        };

        Runtime.MarshalManagedException += (_, _) =>
        {
            Console.WriteLine("In MarshalManagedException Handler");
        };

        Runtime.MarshalObjectiveCException += (_, _) =>
        {
            Console.WriteLine("In MarshalObjectiveCException Handler");
        };

        return true;
    }
}
