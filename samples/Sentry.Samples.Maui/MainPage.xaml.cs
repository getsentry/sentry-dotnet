#pragma warning disable CS0618

using Microsoft.Extensions.Logging;

namespace Sentry.Samples.Maui;

public partial class MainPage
{
    private readonly ILogger<MainPage> _logger;

    int count = 0;

    // NOTE: You can only inject an ILogger<T>, not a plain ILogger
    public MainPage(ILogger<MainPage> logger)
    {
#if !ANDROID
        JavaCrashBtn.IsVisible = false;
#endif

#if !(ANDROID || IOS) // TODO: Enable for MACCATALYST
        NativeCrashBtn.IsVisible = false;
#endif
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
        SentrySdk.CauseCrash(CrashType.Managed);
    }

    private void OnCapturedExceptionClicked(object sender, EventArgs e)
    {
        try
        {
            SentrySdk.CauseCrash(CrashType.Managed);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
        }
    }

    private void OnJavaCrashClicked(object sender, EventArgs e)
    {
#if ANDROID
        SentrySdk.CauseCrash(CrashType.Java);
#endif
    }

    private void OnNativeCrashClicked(object sender, EventArgs e)
    {
#if ANDROID || IOS // TODO: Enable for MACCATALYST
        SentrySdk.CauseCrash(CrashType.Native);
#endif
    }
}

