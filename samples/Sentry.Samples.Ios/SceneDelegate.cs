namespace Sentry.Samples.Ios;

[Register("SceneDelegate")]
public class SceneDelegate : UIResponder, IUIWindowSceneDelegate
{

    [Export("window")] public UIWindow? Window { get; set; }

    [Export("scene:willConnectToSession:options:")]
    public void WillConnect(UIScene scene, UISceneSession session, UISceneConnectionOptions connectionOptions)
    {
        if (scene is not UIWindowScene windowScene)
        {
            return;
        }

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

        Window = new UIWindow(windowScene) { RootViewController = vc };
        Window.MakeKeyAndVisible();
    }

    [Export("sceneDidDisconnect:")]
    public void DidDisconnect(UIScene scene)
    {
        // Called as the scene is being released by the system.
        // This occurs shortly after the scene enters the background, or when its session is discarded.
        // Release any resources associated with this scene that can be re-created the next time the scene connects.
        // The scene may re-connect later, as its session was not neccessarily discarded (see UIApplicationDelegate `DidDiscardSceneSessions` instead).
    }

    [Export("sceneDidBecomeActive:")]
    public void DidBecomeActive(UIScene scene)
    {
        // Called when the scene has moved from an inactive state to an active state.
        // Use this method to restart any tasks that were paused (or not yet started) when the scene was inactive.
    }

    [Export("sceneWillResignActive:")]
    public void WillResignActive(UIScene scene)
    {
        // Called when the scene will move from an active state to an inactive state.
        // This may occur due to temporary interruptions (ex. an incoming phone call).
    }

    [Export("sceneWillEnterForeground:")]
    public void WillEnterForeground(UIScene scene)
    {
        // Called as the scene transitions from the background to the foreground.
        // Use this method to undo the changes made on entering the background.
    }

    [Export("sceneDidEnterBackground:")]
    public void DidEnterBackground(UIScene scene)
    {
        // Called as the scene transitions from the foreground to the background.
        // Use this method to save data, release shared resources, and store enough scene-specific state information
        // to restore the scene back to its current state.
    }
}
