using Microsoft.Extensions.Logging;

namespace Sentry.Samples.Maui;

public partial class MainPage
{
    private readonly ILogger<MainPage> _logger;

    int count = 0;

    // NOTE: You can only inject an ILogger<T>, not a plain ILogger
    public MainPage(ILogger<MainPage> logger)
    {
        _logger = logger;
        InitializeComponent();
    }

    private void OnCounterClicked(object sender, EventArgs e)
    {
        count++;

        if (count == 1)
            CounterBtn.Text = $"Clicked {count} time";
        else
            CounterBtn.Text = $"Clicked {count} times";

        SemanticScreenReader.Announce(CounterBtn.Text);

        _logger.LogInformation("The button has been clicked {ClickCount} times", count);
    }

    private void OnUnhandledExceptionClicked(object sender, EventArgs e)
    {
        throw new Exception("This is an unhanded test exception, thrown from managed code in a MAUI app!");
    }

    private void OnCapturedExceptionClicked(object sender, EventArgs e)
    {
        try
        {
            throw new Exception("This is a captured test exception, thrown from managed code in a MAUI app!");
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
        }
    }
}

