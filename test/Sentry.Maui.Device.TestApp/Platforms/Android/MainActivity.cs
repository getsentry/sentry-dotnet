using Android.App;
using Android.Content.PM;
using Android.OS;

namespace Sentry.Maui.Device.TestApp;

public partial class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        Microsoft.Maui.ApplicationModel.ActivityStateManager.Default.Init(this, savedInstanceState);
    }
}
