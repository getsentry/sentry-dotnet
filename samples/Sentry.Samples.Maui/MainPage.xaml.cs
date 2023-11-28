using Microsoft.Extensions.Logging;

namespace Sentry.Samples.Maui;

public partial class MainPage
{
    private readonly ILogger<MainPage> _logger;

    private int _count = 0;

    // NOTE: You can only inject an ILogger<T>, not a plain ILogger
    public MainPage(ILogger<MainPage> logger)
    {
        _logger = logger;
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        AddHandlerBreadcrumbs();

#if !ANDROID
        JavaCrashBtn.IsVisible = false;
#endif

#if !__MOBILE__
        NativeCrashBtn.IsVisible = false;
#endif
        base.OnAppearing();
    }

    private void AddHandlerBreadcrumbs()
    {
        foreach (var visualElement in this.GetVisualTreeDescendants())
        {
            if (visualElement is Element element)
            {
                element.HandlerChanged += (sender, _) =>
                {
                    // The StyleId correlates to the element's name if one is set in XAML
                    var elementName = element.StyleId;
                    if (elementName is not null)
                    {
                        var type = element.Handler?.GetType();
                        var handlerName = type is not null ? type.Name : string.Empty;

                        SentrySdk.AddBreadcrumb($"'{elementName}' handler changed to '{handlerName}'", "system", "ui.handlers");
                    }
                };
            }
        }
    }

    private void OnCounterClicked(object sender, EventArgs e)
    {
        _count++;

        if (_count == 1)
        {
            CounterBtn.Text = $"Clicked {_count} time";
        }
        else
        {
            CounterBtn.Text = $"Clicked {_count} times";
        }

        SemanticScreenReader.Announce(CounterBtn.Text);

        _logger.LogInformation("The button has been clicked {ClickCount} times", _count);
    }

    private void OnUnhandledExceptionClicked(object sender, EventArgs e)
    {
#pragma warning disable CS0618
        SentrySdk.CauseCrash(CrashType.Managed);
#pragma warning restore CS0618
    }

    private void OnBackgroundThreadUnhandledExceptionClicked(object sender, EventArgs e)
    {
#pragma warning disable CS0618
        SentrySdk.CauseCrash(CrashType.ManagedBackgroundThread);
#pragma warning restore CS0618
    }

    private void OnCapturedExceptionClicked(object sender, EventArgs e)
    {
        try
        {
            throw new ApplicationException("This exception was thrown and captured manually, without crashing the app.");
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
        }
    }
    private void OnJavaCrashClicked(object sender, EventArgs e)
    {
#if ANDROID
#pragma warning disable CS0618
        SentrySdk.CauseCrash(CrashType.Java);
#pragma warning restore CS0618
#endif
    }

    private void OnNativeCrashClicked(object sender, EventArgs e)
    {
#if __MOBILE__
#pragma warning disable CS0618
        SentrySdk.CauseCrash(CrashType.Native);
#pragma warning restore CS0618
#endif
    }
}
