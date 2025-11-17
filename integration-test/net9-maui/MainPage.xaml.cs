namespace Sentry.Maui.Device.IntegrationTestApp;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        App.OnAppearing();
    }
}
