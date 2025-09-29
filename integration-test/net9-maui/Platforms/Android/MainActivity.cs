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

        System.Environment.SetEnvironmentVariable("SENTRY_CRASH_TYPE", Intent?.GetStringExtra("SENTRY_CRASH_TYPE"));
        System.Environment.SetEnvironmentVariable("SENTRY_TEST_ACTION", Intent?.GetStringExtra("SENTRY_TEST_ACTION"));
    }
}
