using System.Diagnostics.Tracing;

namespace Sentry.Samples.Console.Metrics;

[EventSource(Name = IterationEventCounterSource.EventSourceName)]
public sealed class IterationEventCounterSource : EventSource
{
    internal const string EventSourceName = "Sentry.Samples.Console.Metrics.IterationEventCounterSource";
    public static readonly IterationEventCounterSource Log = new IterationEventCounterSource();

    [Event(1, Message = "One more loop...", Level = EventLevel.Informational)]
    public void AddLoopCount()
    {
        WriteEvent(1);
    }
}
