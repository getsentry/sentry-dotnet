using Foundation;
using UIKit;

namespace Sentry.Maui.Device.IntegrationTestApp;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    public override void OnActivated(UIApplication application)
    {
        base.OnActivated(application);
        App.OnActivated();
    }
}
