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

#pragma warning disable CS0618
        if (Enum.TryParse<CrashType>(App.TestArg, ignoreCase: true, out var crashType))
        {
            SentrySdk.CauseCrash(crashType);
        }
#pragma warning restore CS0618

        if (App.HasTestArg("NullReferenceException"))
        {
            try
            {
                object? obj = null;
                _ = obj!.ToString();
            }
            catch (NullReferenceException ex)
            {
                SentrySdk.CaptureException(ex);
            }
            App.Kill();
        }
        else if (App.HasTestArg("None"))
        {
            App.Kill();
        }
    }
}
