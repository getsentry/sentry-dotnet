#if ANDROID
using Android.OS;
#endif

namespace Sentry.Maui.Device.IntegrationTestApp;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        var testArg = System.Environment.GetEnvironmentVariable("SENTRY_TEST_ARG");

#pragma warning disable CS0618
        if (Enum.TryParse<CrashType>(testArg, ignoreCase: true, out var crashType))
        {
            SentrySdk.CauseCrash(crashType);
        }
#pragma warning restore CS0618

        if (testArg?.Equals("NullReferenceException", StringComparison.OrdinalIgnoreCase) == true)
        {
            try
            {
                object? obj = null;
                _ = obj!.ToString();
            }
            catch (NullReferenceException ex)
            {
                SentrySdk.CaptureException(ex);
            }
        }

        SentrySdk.Close();
#if ANDROID
        // prevent auto-restart
        Platform.CurrentActivity?.FinishAffinity();
        Process.KillProcess(Process.MyPid());
#elif IOS
        System.Environment.Exit(0);
#endif
    }
}
