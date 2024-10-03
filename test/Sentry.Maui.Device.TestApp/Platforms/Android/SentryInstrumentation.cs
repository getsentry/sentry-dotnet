using Android.App;
using Android.Runtime;
using DeviceRunners.XHarness.Maui;
using Environment = System.Environment;

namespace Sentry.Maui.Device.TestApp;

[Instrumentation(Name = "Sentry.Maui.Device.TestApp.SentryInstrumentation")]
public class SentryInstrumentation : XHarnessInstrumentation
{
    protected SentryInstrumentation(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
    }

    public override void OnStart()
    {
        try
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
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        base.OnStart();
    }

    private bool IsGitHubActions => Arguments is { } bundle && bundle.GetString("IsGitHubActions") == "true";
}
