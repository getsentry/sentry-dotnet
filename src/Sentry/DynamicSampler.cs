#if NET8_0_OR_GREATER
using System.Diagnostics.Metrics;
using Sentry.Extensibility;

namespace Sentry;

/// <summary>
/// Custom sampler that adjusts the sample rate based on the number of discarded envelopes and open queue slots.
/// </summary>
public class DynamicSampler: IDisposable
{
    private readonly SentryOptions _options;
    private readonly int _envelopesDiscardedThreshold;
    private readonly int _openQueueSlotsThreshold;

    private readonly CancellationTokenSource _shutdownSource;

    private double _sampleRate = 1.0;
    private readonly ReaderWriterLockSlim _sampleLock = new();

    private int _discardedEnvelopes = 0;

    private List<int> _openQueueSlotObservations = new();
    private readonly object _openQueueSlotObservationsLock = new();

    private volatile bool _disposed;
    private readonly MeterListener _meterListener = new();

    private Task AdjustRateTask { get; }

    /// <summary>
    /// Creates a dynamic sampler
    /// </summary>
    /// <param name="options">A <see cref="SentryOptions"/> instance</param>
    /// <param name="envelopesDiscardedThreshold">If the number of discarded envelopes in a given period is above this number, the sample rate will be adjusted down</param>
    /// <param name="openQueueSlotsThreshold">If the average number of open queue slots is below this number in a given sample period, the sample rate will be adjusted up</param>
    /// <param name="shutdownSource">A cancellation token for the sample update task</param>
    public DynamicSampler(SentryOptions options, int envelopesDiscardedThreshold = 1,
        int openQueueSlotsThreshold = 10, CancellationTokenSource? shutdownSource = null)
    {
        _options = options;
        _envelopesDiscardedThreshold = envelopesDiscardedThreshold;
        _openQueueSlotsThreshold = openQueueSlotsThreshold;
        _shutdownSource = shutdownSource ?? new CancellationTokenSource();
        _meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name is "Sentry.Internal.Backgroundworker")
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };

        _meterListener.SetMeasurementEventCallback<int>(OnMeasurementRecorded);

        // Start the meterListener, enabling InstrumentPublished callbacks.
        _meterListener.Start();

        AdjustRateTask = Task.Run(AdjustRateAsync);
    }

    private void OnMeasurementRecorded(Instrument instrument, int measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
    {
        switch (instrument.Name)
        {
            case "sentry.open_queue_slots":
                lock (_openQueueSlotObservationsLock)
                {
                    _openQueueSlotObservations.Add(measurement);
                }
                break;
            case "sentry.envelopes_discarded":
                Interlocked.Increment(ref _discardedEnvelopes);
                break;
        }
    }

    private async Task AdjustRateAsync()
    {
        while (!_shutdownSource.Token.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), _shutdownSource.Token).ConfigureAwait(false);

            // Check if the rate should be adjusted down (e.g. if we're discarding too many envelopes)
            var discardedEnvelopes = Interlocked.Exchange(ref _discardedEnvelopes, 0);
            if (discardedEnvelopes > _envelopesDiscardedThreshold)
            {
                _sampleLock.EnterWriteLock();
                try
                {
                    // Sample half as many events
                    _sampleRate /= 2;
                    _options.LogDebug("Sample rate lowered to {0}", _sampleRate);
                }
                finally
                {
                    _sampleLock.ExitWriteLock();
                }
                continue;
            }

            // Check if the rate should be adjusted up (e.g. if we're seeing lots of open queue slots)
            double? observedMean = null;
            lock (_openQueueSlotObservations)
            {
                if (_openQueueSlotObservations.Any())
                {
                    observedMean = _openQueueSlotObservations.Average();
                    _openQueueSlotObservations = new();
                }
            }
            if (observedMean is { } openQueueSlots && openQueueSlots > _openQueueSlotsThreshold)
            {
                _sampleLock.EnterUpgradeableReadLock();
                try
                {
                    if (_sampleRate < 1.0)
                    {
                        _sampleLock.EnterWriteLock();
                        try
                        {
                            // Sample twice as many events
                            _sampleRate = Math.Min(1, _sampleRate * 2);
                            _options.LogDebug("Sample rate increased to {0}", _sampleRate);
                        }
                        finally
                        {
                            _sampleLock.ExitWriteLock();
                        }
                    }
                }
                finally
                {
                    _sampleLock.ExitUpgradeableReadLock();
                }
            }
        }
    }

    /// <summary>
    /// Returns the current sample rate
    /// </summary>
    /// <param name="context">The <see cref="TransactionSamplingContext"/></param>
    /// <returns></returns>
    public double? SampleRate(TransactionSamplingContext context)
    {
        _sampleLock.EnterReadLock();
        try
        {
            return _sampleRate;
        }
        finally
        {
            _sampleLock.ExitReadLock();
        }
    }

    /// <inheritdoc cref="IDisposable.Dispose"/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        try
        {
            _shutdownSource.Cancel();
            AdjustRateTask.Wait();
        }
        catch (OperationCanceledException)
        {
            _options.LogDebug("Stopping the background worker due to a cancellation.");
        }
        catch (Exception exception)
        {
            _options.LogError(exception, "Stopping the background worker threw an exception.");
        }
        finally
        {
            _shutdownSource.Dispose();
            _sampleLock.Dispose();
        }
    }
}
#endif
