using Android.OS;
using Android.Runtime;
using Sentry;
using Sentry.Protocol;

namespace Sentry.Samples.Android
{
    [Activity(Label = "@string/app_name", MainLauncher = true)]
    public class MainActivity : Activity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            SentrySdk.Init(o =>
            {
                o.Debug = true;
                o.Dsn = "https://eb18e953812b41c3aeb042e666fd3b5c@o447951.ingest.sentry.io/5428537";
                o.BeforeSend += @event =>
                {
#pragma warning disable 618
                    @event.Contexts.Device.Architecture = Build.CpuAbi;
#pragma warning restore 618
                    @event.Contexts.Device.Manufacturer = Build.Manufacturer;
                    return @event;
                };
            });
            AndroidEnvironment.UnhandledExceptionRaiser += AndroidEnvironment_UnhandledExceptionRaiser;

            base.OnCreate(savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            var captureException = (Button)base.FindViewById(Resource.Id.captureException)!;
            var throwUnhandledException = (Button)base.FindViewById(Resource.Id.throwUnhandledException)!;
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
            throwUnhandledException.Click += (s, a) => throw new Exception("Unhandled");
        }

        private void AndroidEnvironment_UnhandledExceptionRaiser(object? _, RaiseThrowableEventArgs e)
        {
            e.Exception.Data[Mechanism.HandledKey] = e.Handled;
            e.Exception.Data[Mechanism.MechanismKey] = "UnhandledExceptionRaiser";
            SentrySdk.CaptureException(e.Exception);
            if (!e.Handled)
            {
                SentrySdk.Close();
            }
        }
    }
}
