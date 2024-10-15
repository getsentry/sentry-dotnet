namespace Sentry;

/// <summary>
/// Delegate that determines whether a heap dump should be triggered or not.
/// </summary>
/// <param name="usedMemory">Memory currently used by the process</param>
/// <param name="totalMemory">Total available memory</param>
/// <returns><see langword="true"/> if the heap dump should be triggered; otherwise, <see langword="false"/>.</returns>
public delegate bool HeapDumpTrigger(long usedMemory, long totalMemory);

internal static class HeapDumpTriggers
{
    internal static HeapDumpTrigger MemoryPercentageThreshold(int memoryPercentageThreshold)
    {
        if (memoryPercentageThreshold is < 0 or > 100)
        {
            throw new ArgumentException("Must be a value between 0 and 100", nameof(memoryPercentageThreshold));
        }

        return (long usedMemory, long totalMemory) =>
        {
            var portion = (double)memoryPercentageThreshold / 100;
            var thresholdBytes = (long)Math.Ceiling(portion * totalMemory);
            return usedMemory > thresholdBytes;
        };
    }}
