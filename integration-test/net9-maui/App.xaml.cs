#if ANDROID
using Android.OS;
#endif

namespace Sentry.Maui.Device.IntegrationTestApp;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
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
