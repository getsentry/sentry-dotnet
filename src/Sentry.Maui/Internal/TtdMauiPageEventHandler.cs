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

            // TODO: what about time to full display - it should happen WHEN this finishes
            await _transaction.WaitForLastSpanToFinishAsync(cts.Token).ConfigureAwait(false);
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


/// <summary>
/// TDOO
/// </summary>
public static class Tester
{
    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async ValueTask WaitForLastSpanToFinishAsync(this ITransactionTracer transaction, CancellationToken cancellationToken = default)
    {
        if (transaction.IsAllSpansFinished())
        {
            var span = transaction.GetLastFinishedSpan();
            if (span != null)
                transaction.Finish(span.EndTimestamp);
        }
        else
        {
            var span = await transaction.GetLastSpanWhenFinishedAsync(cancellationToken).ConfigureAwait(false);
            if (span != null)
                transaction.Finish(span.EndTimestamp);
        }
    }

    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="transaction"></param>
    /// <returns></returns>
    public static bool IsAllSpansFinished(this ITransactionTracer transaction)
        => transaction.Spans.All(x => x.IsFinished);


    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="transaction"></param>
    /// <returns></returns>
    public static ISpan? GetLastFinishedSpan(this ITransactionTracer transaction)
        => transaction.Spans
            .ToList()
            .Where(x => x.IsFinished)
            .OrderByDescending(x => x.EndTimestamp)
            .LastOrDefault(x => x.IsFinished);

    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<ISpan?> GetLastSpanWhenFinishedAsync(this ITransactionTracer transaction, CancellationToken cancellationToken = default)
    {
        // what if no spans
        if (transaction.IsAllSpansFinished())
            return transaction.GetLastFinishedSpan();

        var tcs = new TaskCompletionSource<ISpan?>();
        var handler = new EventHandler<SpanStatus?>((_, _) =>
        {
            if (transaction.IsAllSpansFinished())
            {
                var lastSpan = transaction.GetLastFinishedSpan();
                tcs.SetResult(lastSpan);
            }
        });

        try
        {
            foreach (var span in transaction.Spans)
            {
                if (!span.IsFinished)
                {
                    span.StatusChanged += handler;
                }
            }

            return await tcs.Task.ConfigureAwait(false);
        }
        finally
        {
            foreach (var span in transaction.Spans)
            {
                span.StatusChanged -= handler;
            }
        }
    }
}
