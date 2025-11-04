#if ANDROID
using Android.OS;
#endif

namespace Sentry.Maui.Device.IntegrationTestApp;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        PageAppearing += (s, e) =>
        {
#pragma warning disable CS0618
            if (Enum.TryParse<CrashType>(TestArg, ignoreCase: true, out var crashType))
            {
                SentrySdk.CauseCrash(crashType);
            }
#pragma warning restore CS0618

            if (App.HasTestArg("NullReferenceException"))
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
                App.Kill();
            }
            else if (HasTestArg("None"))
            {
                App.Kill();
            }
        };
    }

    public static string? TestArg => System.Environment.GetEnvironmentVariable("SENTRY_TEST_ARG");

    public static bool HasTestArg(string arg)
    {
        return string.Equals(TestArg, arg, StringComparison.OrdinalIgnoreCase);
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }

    public static void Kill()
    {
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
