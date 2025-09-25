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
        var crashTypeEnv = Environment.GetEnvironmentVariable("SENTRY_CRASH_TYPE");
        if (Enum.TryParse<CrashType>(crashTypeEnv, ignoreCase: true, out var crashType))
        {
            SentrySdk.CauseCrash(crashType);
        }
#pragma warning restore CS0618

        var testActionEnv = Environment.GetEnvironmentVariable("SENTRY_TEST_ACTION");
        if (testActionEnv?.Equals("NullReferenceException", StringComparison.OrdinalIgnoreCase) == true)
        {
            try
            {
                object? obj = null;
                _ = obj.ToString();
            }
            catch (NullReferenceException ex)
            {
                SentrySdk.CaptureException(ex);
            }
        }

        SentrySdk.Flush();
        Environment.Exit(0);
    }
}
