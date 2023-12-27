using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.EventPipe;

/// <summary>
/// Reduce sampling rate from 1 Hz that is the default for the provider to the configured SamplingRateMs.
/// </summary>
internal class Downsampler
{
    private static double _samplingRateMs = (double)1_000 / 101; // 101 Hz

    // Maps from ThreadIndex to the last sample timestamp for that thread.
    private GrowableArray<double> _prevThreadSamples = new(10);

    public void NewThreadAdded(int threadIndex)
    {
        if (threadIndex >= _prevThreadSamples.Count)
        {
            _prevThreadSamples.Count = threadIndex + 1;
            _prevThreadSamples[threadIndex] = double.MinValue;
        }
    }

    public bool ShouldSample(int threadIndex, double timestampMs)
    {
        Debug.Assert(threadIndex < _prevThreadSamples.Count, "ThreadIndex too large - you must call NewThreadAdded() if a new thread is added.");
        if (_prevThreadSamples[threadIndex] + _samplingRateMs <= timestampMs)
        {
            _prevThreadSamples[threadIndex] = timestampMs;
            return true;
        }
        return false;
    }
}
