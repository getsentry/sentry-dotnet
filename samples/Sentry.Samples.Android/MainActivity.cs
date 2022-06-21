using Sentry.Samples.Android.Kotlin;

namespace Sentry.Samples.Android
{
    [Activity(Label = "@string/app_name", MainLauncher = true)]
    public class MainActivity : Activity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            SentrySdk.Init(this, o =>
            {
                o.Dsn = "https://eb18e953812b41c3aeb042e666fd3b5c@o447951.ingest.sentry.io/5428537";
                o.SendDefaultPii = true; // adds the user's IP address automatically
            });

            // Here's an example of adding custom scope information.
            // This can be done at any time, and will be passed through to the Java SDK as well.
            SentrySdk.ConfigureScope(scope =>
            {
                scope.AddBreadcrumb("Custom Breadcrumb");
                scope.SetExtra("Test", "Custom Extra Data");
                scope.User = new User
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
            var throwKotlinException = (Button)base.FindViewById(Resource.Id.throwKotlinException)!;
            throwKotlinException.Click += (s, a) => Buggy.Throw();
            var throwKotlinExceptionBackgroundThread = (Button)base.FindViewById(Resource.Id.throwKotlinExceptionBackgroundThread)!;
            throwKotlinExceptionBackgroundThread.Click += (s, a) => Buggy.ThrowOnBackgroundThread();
            var crashInC = (Button)base.FindViewById(Resource.Id.crashInC)!;
            crashInC.Click += (s, a) => CrashInC();
        }

        [System.Runtime.InteropServices.DllImport("libbuggy.so", EntryPoint = "crash_in_c")]
        private static extern void CrashInC();
    }
}
