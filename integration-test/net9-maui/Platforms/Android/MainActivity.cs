using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace Sentry.Maui.Device.IntegrationTestApp;

[Activity(
    Name = "io.sentry.dotnet.maui.device.integrationtestapp.MainActivity",
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density
)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        System.Environment.SetEnvironmentVariable("SENTRY_DSN", Intent?.GetStringExtra("SENTRY_DSN"));
        System.Environment.SetEnvironmentVariable("SENTRY_TEST_ARG", Intent?.GetStringExtra("SENTRY_TEST_ARG"));
    }

    protected override void OnStop()
    {
        base.OnStop();

        if (App.HasTestArg("Background"))
        {
            SentrySdk.CaptureMessage("Background");
            App.Kill();
        }
    }
}
