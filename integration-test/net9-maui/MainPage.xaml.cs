namespace Sentry.Maui.Device.IntegrationTestApp;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();

        this.Loaded += (s, e) =>
        {
            App.OnAppearing();
        };
    }
}
