using System;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Sentry.Samples.Uwp
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            BrokenLogic();
        }

        private void Message_Click(object sender, RoutedEventArgs e)
        {
            SentrySdk.CaptureMessage("Hello UWP");
            _ = new MessageDialog("Hello UWP").ShowAsync();

        }

        private void BrokenLogic()
        {
            object data = Background;
            object data2 = Height;
            var brokenData = (int)data + (int)data2;
            Height = -brokenData;
        }
    }
}
