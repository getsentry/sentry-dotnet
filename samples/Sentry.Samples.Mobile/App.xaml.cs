using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Sentry.Samples.Mobile.Services;
using Sentry.Samples.Mobile.Views;

namespace Sentry.Samples.Mobile
{
    public partial class App : Application
    {

        public App()
        {
            SentrySdk.Init(o =>
            {
                o.Debug = false;
                o.Dsn = new Dsn("https://80aed643f81249d4bed3e30687b310ab@o447951.ingest.sentry.io/5428537");
            });

            InitializeComponent();

            DependencyService.Register<MockDataStore>();
            MainPage = new MainPage();
        }

        protected override void OnStart() => SentrySdk.AddBreadcrumb("OnStart",  "app.lifecycle", "event");

        protected override void OnSleep() => SentrySdk.AddBreadcrumb("OnSleep",  "app.lifecycle", "event");

        protected override void OnResume() => SentrySdk.AddBreadcrumb("OnResume",  "app.lifecycle", "event");
    }
}
