using Sentry.Internal;

namespace Sentry.Maui.Internal;

/// <summary>
/// Time-to-(initial/full)-display page event handler
/// https://docs.sentry.io/product/insights/mobile/mobile-vitals/
/// </summary>
internal class TtdMauiPageEventHandler(IHub hub) : IMauiPageEventHandler
{
    internal static long? StartupTimestamp { get; set; }

    // [MobileVital.AppStartCold]: 'duration',
    // [MobileVital.AppStartWarm]: 'duration',
    // [MobileVital.FramesTotal]: 'integer',
    // [MobileVital.FramesSlow]: 'integer',
    // [MobileVital.FramesFrozen]: 'integer',
    // [MobileVital.FramesSlowRate]: 'percentage',
    // [MobileVital.FramesFrozenRate]: 'percentage',
    // [MobileVital.StallCount]: 'integer',
    // [MobileVital.StallTotalTime]: 'duration',
    // [MobileVital.StallLongestTime]: 'duration',
    // [MobileVital.StallPercentage]: 'percentage',

    internal const string LoadCategory = "ui.load";
    internal const string InitialDisplayType = "initial_display";
    internal const string FullDisplayType = "full_display";
    private bool _ttidRan = false; // this should require thread safety
    private ISpan? _timeToInitialDisplaySpan;
    private ITransactionTracer? _transaction;

    /// <inheritdoc />
    public void OnAppearing(Page page)
    {
        if (_ttidRan && StartupTimestamp != null)
            return;

        //DispatchTime.Now.Nanoseconds
        _ttidRan = true;
        var startupTimestamp = ProcessInfo.Instance!.StartupTimestamp;
        var screenName = page.GetType().FullName ?? "root /";
        _transaction = hub.StartTransaction(
            LoadCategory,
            "start"
        );
        var elapsedTime = Stopwatch.GetElapsedTime(startupTimestamp);

        _timeToInitialDisplaySpan = _transaction.StartChild(InitialDisplayType, $"{screenName} initial display");
        _timeToInitialDisplaySpan.SetMeasurement("test", elapsedTime.TotalMilliseconds, MeasurementUnit.Parse("ms"));
        _timeToInitialDisplaySpan.Finish();
    }

    /// <inheritdoc />
    public void OnDisappearing(Page page)
    {
    }

    public void OnNavigatedTo(Page page)
    {
        if (_transaction is { IsFinished: false })
        {
            // TODO: wait for all spans
            _transaction?.Finish();

            _timeToInitialDisplaySpan = null;
            _transaction = null;
        }
    }

    public void OnNavigatedFrom(Page page)
    {
    }
}
