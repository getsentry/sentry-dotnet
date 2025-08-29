namespace Sentry.Internal;

internal static class BackpressureMonitorExtensions
{
    internal static double GetDownsampleFactor(this BackpressureMonitor? monitor) => monitor?.DownsampleFactor ?? 1.0;
}
