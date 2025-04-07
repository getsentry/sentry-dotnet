using Sentry.Internal;
using Sentry.Internal.Extensions;

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
    public async void OnAppearing(Page page)
    {
        if (_ttidRan && StartupTimestamp != null)
            return;

        // if (Interlocked.Exchange<bool>(ref _ttidRan, true))
        //     return;

        //DispatchTime.Now.Nanoseconds
        _ttidRan = true;
        var startupTimestamp = ProcessInfo.Instance!.StartupTimestamp;
        var screenName = page.GetType().FullName ?? "root /";
        _transaction = hub.StartTransaction(
            LoadCategory,
            "start"
        );
        var elapsedTime = Stopwatch.GetElapsedTime(startupTimestamp);

        _timeToInitialDisplaySpan = _transaction.StartChild(InitialDisplayType, $"{screenName} initial display", ProcessInfo.Instance!.StartupTime);
        _timeToInitialDisplaySpan.SetMeasurement("test", elapsedTime.TotalMilliseconds, MeasurementUnit.Parse("ms"));
        _timeToInitialDisplaySpan.Finish();

        // we allow 200ms for the user to start any async tasks with spans
        await Task.Delay(200).ConfigureAwait(false);

        try
        {
            using var cts = new CancellationTokenSource();
            cts.CancelAfterSafe(TimeSpan.FromSeconds(30));

            // TODO: grab last span and add ttfd measurement
            // we're assuming that the user starts any spans around data calls, we wait for those before marking the transaction as finished
            await _transaction.FinishWithLastSpanAsync(cts.Token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // TODO: what to do?
            Console.WriteLine(ex);
        }
    }

    public void OnDisappearing(Page page) { }
    public void OnNavigatedTo(Page page) { }
    public void OnNavigatedFrom(Page page) { }
}

