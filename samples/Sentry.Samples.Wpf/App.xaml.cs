using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Navigation;
using Sentry.Protocol;

namespace Sentry.Samples.Wpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            // TODO: Should be part of Sentry.Wpf and hook automatically
            DispatcherUnhandledException += (sender, e) =>
            {
                if (e.Exception is Exception ex)
                {
                    ex.Data[Mechanism.HandledKey] = e.Handled;
                    ex.Data[Mechanism.MechanismKey] = "App.DispatcherUnhandledException";
                    _ = SentrySdk.CaptureException(ex);
                }

                if (!e.Handled)
                {
                    // Unhandled will crash the app so flush the queue:
                    SentrySdk.FlushAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();
                }
            };

            SentrySdk.Init(o =>
            {
                o.Dsn = "https://eb18e953812b41c3aeb042e666fd3b5c@o447951.ingest.sentry.io/5428537";
                // TODO: Should print to VS debug window (similar to Sentry for ASP.NET)
                o.Debug = true;
                // TODO: Doesn't support multiple instances of the process on the same directory yet
                o.CacheDirectoryPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                // For testing, set to 100% transactions for performance monitoring.
                o.TracesSampleRate = 1.0;
            });
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            SentrySdk.AddBreadcrumb("OnStartup", "app.lifecycle");
            SentrySdk.ConfigureScope(s => s.Transaction = SentrySdk.StartTransaction("Startup", "app.start"));
            base.OnStartup(e);
        }

        protected override void OnNavigating(NavigatingCancelEventArgs e)
        {
            SentrySdk.AddBreadcrumb("NavigatingCancelEventArgs",
                "navigation",
                data: new Dictionary<string, string>
            {
                {"url", e.Uri.ToString()}
            });
            base.OnNavigating(e);
        }

        protected override void OnFragmentNavigation(FragmentNavigationEventArgs e)
        {
            SentrySdk.AddBreadcrumb("OnFragmentNavigation",
                "navigation",
                data: new Dictionary<string, string>
            {
                {"fragment", e.Fragment},
                {"handled", e.Handled.ToString()}
            });
            base.OnFragmentNavigation(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            SentrySdk.Close();
        }
    }
}
