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
            InitializeComponent();

            DependencyService.Register<MockDataStore>();
            MainPage = new MainPage();
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
