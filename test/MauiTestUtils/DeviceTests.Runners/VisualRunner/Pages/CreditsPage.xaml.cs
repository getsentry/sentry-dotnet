#nullable enable
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace Microsoft.Maui.TestUtils.DeviceTests.Runners.VisualRunner.Pages;

public partial class CreditsPage : ContentPage
{
    public CreditsPage()
    {
        InitializeComponent();
    }

    private void OnNavigating(object? sender, WebNavigatingEventArgs e)
    {
        Browser.OpenAsync(e.Url);

        e.Cancel = true;
    }
}
