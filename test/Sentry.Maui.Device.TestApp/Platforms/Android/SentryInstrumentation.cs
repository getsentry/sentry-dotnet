using Android.App;
using Android.Runtime;
using DeviceRunners.XHarness.Maui;
using Environment = System.Environment;

namespace Sentry.Maui.Device.TestApp;

/// <summary>
/// Decorating this class with the Instrumentation attribute means we can specify it as our instrumentation entry point
/// when launching from XHarness. We can then intercept the OnStart event to set environment variables on Android from
/// arguments passed to the instrumentation. Note that AndroidEnableMarshalMethods also has to be set in the csproj file
/// for this to work.
/// </summary>
[Instrumentation(Name = "Sentry.Maui.Device.TestApp.SentryInstrumentation")]
public class SentryInstrumentation : XHarnessInstrumentation
{
    protected SentryInstrumentation(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
    }

    public override void OnStart()
    {
        Console.WriteLine("Parsing instrumentation arguments");
        if (IsGitHubActions)
        {
            Console.WriteLine("CI build detected - setting environment variables for CI on the device");
            Environment.SetEnvironmentVariable("GITHUB_ACTIONS", "true");
        }
        else
        {
            Console.WriteLine("CI build not detected - no environment variables set");
        }

        base.OnStart();
    }

    private bool IsGitHubActions => Arguments is { } bundle && bundle.GetString("IsGitHubActions") == "true";
}
