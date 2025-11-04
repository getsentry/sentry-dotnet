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
        }
        else if (App.TryGetBreadcrumb(App.TestArg, out var breadcrumb) && breadcrumb.Data != null)
        {
            SentrySdk.CaptureMessage(App.TestArg, scope =>
            {
                scope.SetExtra("category", breadcrumb.Category);
                foreach (var kvp in breadcrumb.Data)
                {
                    scope.SetExtra(kvp.Key, kvp.Value);
                }
                scope.SetExtra("type", breadcrumb.Type);
            });
        }

        App.Kill();
    }
}
