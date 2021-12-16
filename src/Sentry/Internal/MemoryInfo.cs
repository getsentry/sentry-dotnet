#if NETCOREAPP3_0_OR_GREATER

using System;
using System.Text.Json;
using Sentry.Extensibility;

namespace Sentry
{
    sealed class MemoryInfo : IJsonSerializable
    {
        public long AllocatedBytes { get; }
        public long FragmentedBytes { get; }
        public long HeapSizeBytes { get; }
        public long HighMemoryLoadThresholdBytes { get; }
        public long TotalAvailableMemoryBytes { get; }
        public long MemoryLoadBytes { get; }

#if NET5_0_OR_GREATER
        public long TotalCommittedBytes { get; }
        public long PromotedBytes { get; }
        public long PinnedObjectsCount { get; }
        public double PauseTimePercentage { get; }
        public TimeSpan[] PauseDurations { get; }
        public long Index { get; }
        public int Generation { get; }
        public long FinalizationPendingCount { get; }
        public bool Compacted { get; }
        public bool Concurrent { get; }

        public MemoryInfo(
            long allocatedBytes,
            long fragmentedBytes,
            long heapSizeBytes,
            long highMemoryLoadThresholdBytes,
            long totalAvailableMemoryBytes,
            long memoryLoadBytes,
            long totalCommittedBytes,
            long promotedBytes,
            long pinnedObjectsCount,
            double pauseTimePercentage,
            TimeSpan[] pauseDurations,
            long index,
            int generation,
            long finalizationPendingCount,
            bool compacted,
            bool concurrent)
        {
            AllocatedBytes = allocatedBytes;
            FragmentedBytes = fragmentedBytes;
            HeapSizeBytes = heapSizeBytes;
            HighMemoryLoadThresholdBytes = highMemoryLoadThresholdBytes;
            TotalAvailableMemoryBytes = totalAvailableMemoryBytes;
            MemoryLoadBytes = memoryLoadBytes;
            TotalCommittedBytes = totalCommittedBytes;
            PromotedBytes = promotedBytes;
            PinnedObjectsCount = pinnedObjectsCount;
            PauseTimePercentage = pauseTimePercentage;
            PauseDurations = pauseDurations;
            Index = index;
            Generation = generation;
            FinalizationPendingCount = finalizationPendingCount;
            Compacted = compacted;
            Concurrent = concurrent;
        }
#else
        public MemoryInfo(
            long allocatedBytes,
            long fragmentedBytes,
            long heapSizeBytes,
            long highMemoryLoadThresholdBytes,
            long totalAvailableMemoryBytes,
            long memoryLoadBytes)
        {
            AllocatedBytes = allocatedBytes;
            FragmentedBytes = fragmentedBytes;
            HeapSizeBytes = heapSizeBytes;
            HighMemoryLoadThresholdBytes = highMemoryLoadThresholdBytes;
            TotalAvailableMemoryBytes = totalAvailableMemoryBytes;
            MemoryLoadBytes = memoryLoadBytes;
        }
#endif
        public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
        {
            writer.WriteStartObject();

            writer.WriteNumber("allocatedBytes", AllocatedBytes);
            writer.WriteNumber("fragmentedBytes", FragmentedBytes);
            writer.WriteNumber("heapSizeBytes", HeapSizeBytes);
            writer.WriteNumber("highMemoryLoadThresholdBytes", HighMemoryLoadThresholdBytes);
            writer.WriteNumber("totalAvailableMemoryBytes", TotalAvailableMemoryBytes);
            writer.WriteNumber("memoryLoadBytes", MemoryLoadBytes);

#if NET5_0_OR_GREATER
            writer.WriteNumber("totalCommittedBytes", TotalCommittedBytes);
            writer.WriteNumber("promotedBytes", PromotedBytes);
            writer.WriteNumber("pinnedObjectsCount", PinnedObjectsCount);
            writer.WriteNumber("pauseTimePercentage", PauseTimePercentage);
            writer.WriteNumber("index", Index);
            writer.WriteNumber("generation", Generation);
            writer.WriteNumber("finalizationPendingCount", FinalizationPendingCount);
            writer.WriteBoolean("compacted", Compacted);
            writer.WriteBoolean("concurrent", Concurrent);
            writer.WriteStartArray();
            writer.WritePropertyName("pauseDurations");
            foreach (var duration in PauseDurations)
            {
                writer.WriteNumberValue(duration.TotalMilliseconds);
            }
            writer.WriteEndArray();
#endif
            writer.WriteEndObject();
        }
    }
}

#endif
