namespace Sentry.Maui.Device.TestApp;

public partial class MainApplication : MauiApplication
{
    public override void OnCreate()
    {
        base.OnCreate();

        Platform.Init(this);
        Microsoft.Maui.ApplicationModel.ActivityStateManager.Default.Init(this);
    }
}
