using Microsoft.UI.Xaml;
using Windows.Graphics;
using Sentry;

namespace winui_app
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            AppWindow.Resize(new SizeInt32(800, 600));
        }

        private void OnManagedCrashClick(object sender, RoutedEventArgs e)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            SentrySdk.CauseCrash(CrashType.Managed);
#pragma warning restore CS0618
        }

        private void OnNativeCrashClick(object sender, RoutedEventArgs e)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            SentrySdk.CauseCrash(CrashType.Native);
#pragma warning restore CS0618
        }
    }
}
