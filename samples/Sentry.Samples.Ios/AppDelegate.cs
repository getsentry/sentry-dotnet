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
            options.SampleRate = 1.0F;
            options.TracesSampleRate = 1.0;
            options.ProfilesSampleRate = 1.0;

            // All the native iOS SDK options are available below
            // https://docs.sentry.io/platforms/apple/guides/ios/configuration/
            // Enable Native iOS SDK App Hangs detection
            options.Native.EnableAppHangTracking = true;

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

        // create a new window instance based on the screen size
        Window = new UIWindow(UIScreen.MainScreen.Bounds);

        // determine control colours (SystemBackground requires iOS >= 13.0)
        var backgroundColor = UIDevice.CurrentDevice.CheckSystemVersion(13, 0)
#pragma warning disable CA1416
            ? UIColor.SystemBackground
#pragma warning restore CA1416
            : UIColor.White;
        var buttonConfig = UIButtonConfiguration.TintedButtonConfiguration;
        var terminalButtonConfig = UIButtonConfiguration.TintedButtonConfiguration;
        terminalButtonConfig.BaseBackgroundColor = UIColor.SystemRed;

        var vc = new UIViewController();

        var label = new UILabel
        {
            BackgroundColor = backgroundColor,
            TextAlignment = UITextAlignment.Center,
            Text = "Hello, iOS!",
            AutoresizingMask = UIViewAutoresizing.All
        };

        // UIButton for a managed exception that we'll catch and handle (won't crash the app)
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
                throw new Exception("Catch this!");
            }
            catch (Exception e)
            {
                SentrySdk.CaptureException(e);
            }
        };

        // UIButton for unhandled managed exception
        var unhandledCrashButton = new UIButton(UIButtonType.RoundedRect)
        {
            AutoresizingMask = UIViewAutoresizing.All,
            Configuration = terminalButtonConfig
        };
        unhandledCrashButton.SetTitle("Unhandled Crash", UIControlState.Normal);
        unhandledCrashButton.TouchUpInside += delegate
        {
            Console.WriteLine("Unhandled Crash button clicked!");
            string s = null!;
            // This will cause a NullReferenceException that will crash the app before Sentry can send the event.
            // Since we're using a caching transport though, the exception will be written to disk and sent the
            // next time the app is launched.
            Console.WriteLine("Length: {0}", s.Length);
        };

        // UIButton for native crash
        var nativeCrashButton = new UIButton(UIButtonType.System)
        {
            Configuration = terminalButtonConfig
        };
        nativeCrashButton.SetTitle("Native Crash", UIControlState.Normal);
        nativeCrashButton.TouchUpInside += delegate
        {
            Console.WriteLine("Native Crash button clicked!");
#pragma warning disable CS0618 // Type or member is obsolete
            // This will cause a native crash that will crash the application before
            // Sentry gets a chance to send the event. Since we've enabled caching however,
            // the event will be written to disk and sent the next time the app is launched.
            SentrySdk.CauseCrash(CrashType.Native);
#pragma warning restore CS0618 // Type or member is obsolete
        };

        // create a UIStackView to hold the label and buttons
        var stackView = new UIStackView(new UIView[] { label, managedCrashButton, unhandledCrashButton, nativeCrashButton })
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

        return true;
    }
}
