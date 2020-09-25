using System;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
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

        private void SendExceptionEvent(object sender, EventArgs e)
        {
            try
            {
                throw new Exception("Handled Exception");
            }
            catch (Exception ex)
            {
                SentrySdk.WithScope(s =>
                {
                    s.Contexts["ex"] = new {ToString = ex.ToString()};
                    _ = SentrySdk.CaptureException(ex);
                    _ = DisplayAlert("Error", ex.Message, "Ok");
                });
            }
        }

        private async void TryCatchAsync(object sender, EventArgs e)
        {
            try
            {
                await SomethingAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                SentrySdk.WithScope(s =>
                {
                    s.Contexts["ex"] = new {ToString = ex.ToString()};
                    _ = SentrySdk.CaptureException(ex);
                    _ = DisplayAlert("Error", ex.Message, "Ok");
                });
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private async Task SomethingAsync(CancellationToken token)
        {
            await InnerCode.GiveMeCookieAsync(token);
        }

        private void SendMessageEvent(object sender, EventArgs e)
        {
            var message = "Hello Xamarin.Forms";
            _ = SentrySdk.CaptureMessage(message);
            _ = DisplayAlert("Message captured", message, "Ok");
        }

        private static class InnerCode
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static async Task GiveMeCookieAsync(CancellationToken token)
            {
                await Task.Yield();
                throw new CookieException();
            }
        }
    }
}
