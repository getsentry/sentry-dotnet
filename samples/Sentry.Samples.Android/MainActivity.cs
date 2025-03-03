using Sentry.Android;

namespace Sentry.Samples.Android;

[Activity(Label = "@string/app_name", MainLauncher = true)]
public class MainActivity : Activity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        SentrySdk.Init(options =>
        {
            options.Dsn = "https://eb18e953812b41c3aeb042e666fd3b5c@o447951.ingest.sentry.io/5428537";
            options.SendDefaultPii = true; // adds the user's IP address automatically

            // Android specific .NET features are under the Android properties:
            options.Android.LogCatIntegration = LogCatIntegrationType.Errors; // Get logcat logs for both handled and unhandled errors; default is unhandled only
            options.Android.LogCatMaxLines = 1000; // Defaults to 1000

            // All the native Android SDK options are available below
            // https://docs.sentry.io/platforms/android/configuration/
            // Enable Native Android SDK ANR detection
            options.Native.AnrEnabled = true;

            options.SetBeforeSend(evt =>
            {
                return evt;
            });
        });

        // Here's an example of adding custom scope information.
        // This can be done at any time, and will be passed through to the Java SDK as well.
        SentrySdk.ConfigureScope(scope =>
        {
            scope.AddBreadcrumb("Custom Breadcrumb");
            scope.SetExtra("Test", "Custom Extra Data");
            scope.User = new SentryUser
            {
                Username = "SomeUser",
                Email = "test@example.com",
                Other =
                {
                    ["CustomInfo"] = "Custom User Info"
                }
            };
        });

        base.OnCreate(savedInstanceState);

        // Set our view from the "main" layout resource
        SetContentView(Resource.Layout.activity_main);

        var captureException = (Button)base.FindViewById(Resource.Id.captureException)!;
        captureException.Click += (s, a) =>
        {
            try
            {
                throw new Exception("Try, catch");
            }
            catch (Exception e)
            {
                SentrySdk.CaptureException(e);
            }
        };
        var throwUnhandledException = (Button)base.FindViewById(Resource.Id.throwUnhandledException)!;
        throwUnhandledException.Click += (s, a) => throw new Exception("Unhandled");

        var throwJavaException = (Button)base.FindViewById(Resource.Id.throwJavaException)!;
#pragma warning disable CS0618
        throwJavaException.Click += (s, a) => SentrySdk.CauseCrash(CrashType.Java);
#pragma warning restore CS0618

        var throwJavaExceptionBackgroundThread = (Button)base.FindViewById(Resource.Id.throwJavaExceptionBackgroundThread)!;
#pragma warning disable CS0618
        throwJavaExceptionBackgroundThread.Click += (s, a) => SentrySdk.CauseCrash(CrashType.JavaBackgroundThread);
#pragma warning restore CS0618

        var crashInC = (Button)base.FindViewById(Resource.Id.crashInC)!;
#pragma warning disable CS0618
        crashInC.Click += (s, a) => SentrySdk.CauseCrash(CrashType.Native);
#pragma warning restore CS0618
    }
}
