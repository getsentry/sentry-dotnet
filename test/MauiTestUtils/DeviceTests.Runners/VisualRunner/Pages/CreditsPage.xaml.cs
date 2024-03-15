#nullable enable
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace Microsoft.Maui.TestUtils.DeviceTests.Runners.VisualRunner.Pages
{
    public partial class CreditsPage : ContentPage
    {
        public CreditsPage()
        {
            InitializeComponent();

/* Unmerged change from project 'TestUtils.DeviceTests.Runners(net8.0-ios)'
Before:
		void OnNavigating(object? sender, WebNavigatingEventArgs e)
After:
        private void OnNavigating(object? sender, WebNavigatingEventArgs e)
*/

/* Unmerged change from project 'TestUtils.DeviceTests.Runners(net8.0-maccatalyst)'
Before:
		void OnNavigating(object? sender, WebNavigatingEventArgs e)
After:
        private void OnNavigating(object? sender, WebNavigatingEventArgs e)
*/
        }

        private void OnNavigating(object? sender, WebNavigatingEventArgs e)
        {
            Browser.OpenAsync(e.Url);

            e.Cancel = true;
        }
    }
}
