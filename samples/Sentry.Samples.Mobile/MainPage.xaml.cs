using System;
using Xamarin.Forms;

namespace Sentry.Samples.Mobile
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private void CrashEvent(object sender, EventArgs e)
        {
            throw new Exception("Unhandled Exception");
        }

        private void SendExceptioneEvent(object sender, EventArgs e)
        {
            try
            {
                throw new Exception("Handled Exception");
            }
            catch (Exception ex)
            {
                _ = SentrySdk.CaptureException(ex);
                _ = DisplayAlert("Error", ex.Message, "Ok");
            }
        }

        private void SendMessageEvent(object sender, EventArgs e)
        {
            var message = "Hello Xamarin.Forms";
            _ = SentrySdk.CaptureMessage(message);
            _ = DisplayAlert("Message captured", message, "Ok");
        }


    }
}
