using System;
using System.Windows;

namespace Sentry.Samples.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow() => InitializeComponent();

        private ISpan _initSpan;

        public override void BeginInit()
        {
            _initSpan = SentrySdk.GetSpan()?.StartChild("BeginInit");
            base.BeginInit();
        }

        public override void EndInit()
        {
            base.EndInit();
            _initSpan?.Finish();
            // Is this the API to close the current transaction bound to the scope? Maybe we need to revisit this..
            SentrySdk.ConfigureScope(s => s.Transaction?.Finish());
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
            => throw new InvalidOperationException("This button shall not be pressed!");
    }
}
