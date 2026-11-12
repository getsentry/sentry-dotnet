using Foundation;
using UIKit;

namespace Sentry.Maui.Device.IntegrationTestApp;

[Register("SceneDelegate")]
public class SceneDelegate : MauiUISceneDelegate
{
    public override void OnActivated(UIScene scene)
    {
        base.OnActivated(scene);
        App.OnActivated();
    }
}
