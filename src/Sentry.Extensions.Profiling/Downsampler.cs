using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.EventPipe;

/// <summary>
/// Reduce sampling rate from 1 Hz that is the default for the provider to the configured SamplingRateMs.
/// </summary>
internal class Downsampler
{
    public double SamplingRateMs { get; set; } = (double)1_000 / 101; // 101 Hz
    private double NextSampleCounter = 0;
    private double NextSampleLow = 0;
    private double NextSampleHigh = -1;

    public void AttachTo(EventPipeEventSource source)
    {
        source.AddDispatchHook(DispatchHook);
    }

    // Downsamples to the configured SamplingRateMs by keeping a shifting window of where the timestamp must fall.
    // Alternatively, we could keep a map of the previous sample for each thread and check that instead but that would
    // be a bit less performant (albeit more precise).
    private void DispatchHook(TraceEvent traceEvent, Action<TraceEvent> realDispatch)
    {
        if (traceEvent.ProviderGuid.Equals(SampleProfilerTraceEventParser.ProviderGuid))
        {
            var timestampMs = traceEvent.TimeStampRelativeMSec;
            // Don't sample until the NextSampleLow is reached.
            if (NextSampleLow >= timestampMs)
            {
                NextSampleHigh = -1;
                return; // skip the event
            }

            // This is the first sample after reaching the lower bound - configure the Upper bound to some reasonable value.
            if (NextSampleHigh < 0)
            {
                NextSampleHigh = timestampMs + 0.9;
            }
            // After the upper bound is breached, advance the lower bound to the next window we care about.
            else if (NextSampleHigh < timestampMs)
            {
                NextSampleCounter += 1;
                NextSampleLow = SamplingRateMs * NextSampleCounter - 0.5;
                return; // skip the event
            }
        }

        // Process the event.
        realDispatch(traceEvent);
    }
}