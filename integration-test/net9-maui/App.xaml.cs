#if ANDROID
using Android.OS;
#endif
using System.Collections.Concurrent;

namespace Sentry.Maui.Device.IntegrationTestApp;

public partial class App : Application
{
    private static readonly ConcurrentDictionary<string, Dictionary<string, string>> systemBreadcrumbs = new();
    private static string? testArg;

    public App()
    {
        InitializeComponent();
    }

    public static bool HasTestArg(string arg)
    {
        return string.Equals(testArg, arg, StringComparison.OrdinalIgnoreCase);
    }

    public static void ReceiveSystemBreadcrumb(Breadcrumb breadcrumb)
    {
        if (breadcrumb.Type != "system" ||
            breadcrumb.Data?.TryGetValue("action", out var action) != true ||
            string.IsNullOrEmpty(action))
        {
            return;
        }

        systemBreadcrumbs[action] = new Dictionary<string, string>()
        {
            ["action"] = action,
            ["category"] = breadcrumb.Category ?? string.Empty,
            ["thread_id"] = Thread.CurrentThread.ManagedThreadId.ToString(),
            ["type"] = breadcrumb.Type ?? string.Empty,
        };

        if (HasTestArg(action))
        {
            // received after OnAppearing
            CaptureSystemBreadcrumb(action, systemBreadcrumbs[action]!);
            Kill();
        }
    }

    public static void CaptureSystemBreadcrumb(string action, Dictionary<string, string> data)
    {
        SentrySdk.CaptureMessage(action, scope =>
        {
            foreach (var kvp in data)
            {
                scope.SetExtra(kvp.Key, kvp.Value);
            }
        });
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }

    public static void OnAppearing()
    {
        testArg = System.Environment.GetEnvironmentVariable("SENTRY_TEST_ARG");

#pragma warning disable CS0618
        if (Enum.TryParse<CrashType>(testArg, ignoreCase: true, out var crashType))
        {
            SentrySdk.CauseCrash(crashType);
        }
#pragma warning restore CS0618

        else if (HasTestArg("NullReferenceException"))
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
            Kill();
        }
        else if (!string.IsNullOrEmpty(testArg) && systemBreadcrumbs.TryGetValue(testArg, out var breadcrumb))
        {
            // received before OnAppearing
            CaptureSystemBreadcrumb(testArg, breadcrumb);
            Kill();
        }
        else if (HasTestArg("None"))
        {
            Kill();
        }
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
