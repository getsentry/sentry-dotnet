namespace Sentry.Maui.Device.IntegrationTestApp;

public partial class MainPage : ContentPage
{
    int count = 0;

    public MainPage()
    {
        InitializeComponent();
    }

    private void OnCounterClicked(object? sender, EventArgs e)
    {
        count++;

        if (count == 1)
            CounterBtn.Text = $"Clicked {count} time";
        else
            CounterBtn.Text = $"Clicked {count} times";

        SemanticScreenReader.Announce(CounterBtn.Text);
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
        else if (crashTypeEnv?.Equals("exit", StringComparison.OrdinalIgnoreCase) == true)
        {
            SentrySdk.Flush();
            Environment.Exit(0);
        }
#pragma warning restore CS0618
    }
}
