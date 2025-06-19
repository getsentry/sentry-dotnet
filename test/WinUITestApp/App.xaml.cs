using Microsoft.UI.Xaml;
using Sentry.Protocol;
using System.Security;
using Sentry;

namespace winui_app
{
    public partial class App : Application
    {
        private Window? _window;

        public App()
        {
            SentrySdk.Init(options =>
            {
                options.Dsn = "http://key@127.0.0.1:9999/123";
                options.Debug = true;
                options.IsGlobalModeEnabled = true;
            });
            UnhandledException += OnUnhandledException;

            InitializeComponent();
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _window = new MainWindow();
            _window.Activate();
        }

        [SecurityCritical]
        private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            var exception = e.Exception;
            if (exception != null)
            {
                exception.Data[Mechanism.HandledKey] = false;
                exception.Data[Mechanism.MechanismKey] = "Application.UnhandledException";
                SentrySdk.CaptureException(exception);
                SentrySdk.FlushAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();
            }
        }
    }
}
