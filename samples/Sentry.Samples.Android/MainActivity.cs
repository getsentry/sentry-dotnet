using Sentry.Android;

namespace Sentry.Samples.Android;

[Activity(Label = "@string/app_name", MainLauncher = true)]
public class MainActivity : Activity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        SentrySdk.Init(options =>
        {
#if !SENTRY_DSN_DEFINED_IN_ENV
            // You must specify a DSN. On mobile platforms, this should be done in code here.
            // See https://docs.sentry.io/product/sentry-basics/dsn-explainer/
            options.Dsn = SamplesShared.Dsn;
#else
            // To make things easier for the SDK maintainers our samples check for a SENTRY_DSN environment variable
            // and write this (as a constant) into an EnvironmentVariables class. Generally, you won't want to do
            // this in your own mobile projects though - you should set the DSN in code as above
            options.Dsn = EnvironmentVariables.Dsn;
#endif

            options.SendDefaultPii = true; // adds the user's IP address automatically

            // Android specific .NET features are under the Android properties:
            options.Android.LogCatIntegration = LogCatIntegrationType.Errors; // Get logcat logs for both handled and unhandled errors; default is unhandled only
            options.Android.LogCatMaxLines = 1000; // Defaults to 1000

            // All the native Android SDK options are available below
            // https://docs.sentry.io/platforms/android/configuration/
            // Enable Native Android SDK ANR detection
            options.Native.AnrEnabled = true;

            // Currently experimental support is only available on Android
            options.Native.ExperimentalOptions.SessionReplay.OnErrorSampleRate = 1.0;
            options.Native.ExperimentalOptions.SessionReplay.SessionSampleRate = 1.0;
            options.Native.ExperimentalOptions.SessionReplay.MaskAllImages = false;
            options.Native.ExperimentalOptions.SessionReplay.MaskAllText = false;

            options.SetBeforeSend(evt =>
            {
                if (evt.Exception?.Message.Contains("Something you don't care want logged?") ?? false)
                {
                    return null; // return null to filter out event
                }
                // or add additional data
                evt.SetTag("dotnet-Android-Native-Before", "Hello World");
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
