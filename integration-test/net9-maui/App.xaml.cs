#if ANDROID
using Android.OS;
#endif
using System.Collections.Concurrent;

namespace Sentry.Maui.Device.IntegrationTestApp;

public partial class App : Application
{
    private static readonly ConcurrentDictionary<string, Breadcrumb> _breadcrumbs = new();

    public App()
    {
        InitializeComponent();
    }

    public static string? TestArg => System.Environment.GetEnvironmentVariable("SENTRY_TEST_ARG");

    public static bool HasTestArg(string arg)
    {
        return string.Equals(TestArg, arg, StringComparison.OrdinalIgnoreCase);
    }

    public static void RecordBreadcrumb(Breadcrumb breadcrumb)
    {
        if (breadcrumb.Data?.TryGetValue("action", out var action) != true)
        {
            return;
        }

        _breadcrumbs[action] = new Breadcrumb(
            breadcrumb.Message,
            breadcrumb.Type,
            new Dictionary<string, string>
            {
                ["action"] = action,
                ["thread_id"] = Thread.CurrentThread.ManagedThreadId.ToString()
            },
            breadcrumb.Category,
            breadcrumb.Level);
    }

    public static bool TryGetBreadcrumb(string action, out Breadcrumb? breadcrumb)
    {
        return _breadcrumbs.TryGetValue(action, out breadcrumb);
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
