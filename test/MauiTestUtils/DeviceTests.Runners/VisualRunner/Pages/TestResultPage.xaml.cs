#nullable enable
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Controls;

namespace Microsoft.Maui.TestUtils.DeviceTests.Runners.VisualRunner.Pages
{
    public partial class TestResultPage : ContentPage
    {
        public TestResultPage()
        {
            InitializeComponent();

            CopyMessage.Clicked += CopyMessageClicked;
            CopyTrace.Clicked += CopyTraceClicked;

            /* Unmerged change from project 'TestUtils.DeviceTests.Runners(net8.0-ios)'
            Before:
                    async void CopyMessageClicked(object? sender, System.EventArgs e)
                    {
                        await Clipboard.Default.SetTextAsync(ErrorMessage.Text);
                    }

                    async void CopyTraceClicked(object? sender, System.EventArgs e)
            After:
                    private async void CopyMessageClicked(object? sender, System.EventArgs e)
                    {
                        await Clipboard.Default.SetTextAsync(ErrorMessage.Text);
                    }

                    private async void CopyTraceClicked(object? sender, System.EventArgs e)
            */

            /* Unmerged change from project 'TestUtils.DeviceTests.Runners(net8.0-maccatalyst)'
            Before:
                    async void CopyMessageClicked(object? sender, System.EventArgs e)
                    {
                        await Clipboard.Default.SetTextAsync(ErrorMessage.Text);
                    }

                    async void CopyTraceClicked(object? sender, System.EventArgs e)
            After:
                    private async void CopyMessageClicked(object? sender, System.EventArgs e)
                    {
                        await Clipboard.Default.SetTextAsync(ErrorMessage.Text);
                    }

                    private async void CopyTraceClicked(object? sender, System.EventArgs e)
            */
        }

        private async void CopyMessageClicked(object? sender, System.EventArgs e)
        {
            await Clipboard.Default.SetTextAsync(ErrorMessage.Text);
        }

        private async void CopyTraceClicked(object? sender, System.EventArgs e)
        {
            await Clipboard.Default.SetTextAsync(ErrorTrace.Text);
        }
    }
}
