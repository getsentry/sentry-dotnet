#if NETCOREAPP3_0_OR_GREATER

using System;
using System.Text.Json;
using Sentry.Extensibility;

namespace Sentry
{
    internal sealed class MemoryInfo : IJsonSerializable
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
            long index,
            int generation,
            long finalizationPendingCount,
            bool compacted,
            bool concurrent,
            TimeSpan[] pauseDurations)
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

            writer.WriteNumber("allocated_bytes", AllocatedBytes);
            writer.WriteNumber("fragmented_bytes", FragmentedBytes);
            writer.WriteNumber("heap_size_bytes", HeapSizeBytes);
            writer.WriteNumber("high_memory_load_threshold_bytes", HighMemoryLoadThresholdBytes);
            writer.WriteNumber("total_available_memory_bytes", TotalAvailableMemoryBytes);
            writer.WriteNumber("memory_load_bytes", MemoryLoadBytes);

#if NET5_0_OR_GREATER
            writer.WriteNumber("total_committed_bytes", TotalCommittedBytes);
            writer.WriteNumber("promoted_bytes", PromotedBytes);
            writer.WriteNumber("pinned_objects_count", PinnedObjectsCount);
            writer.WriteNumber("pause_time_percentage", PauseTimePercentage);
            writer.WriteNumber("index", Index);
            writer.WriteNumber("generation", Generation);
            writer.WriteNumber("finalization_pending_count", FinalizationPendingCount);
            writer.WriteBoolean("compacted", Compacted);
            writer.WriteBoolean("concurrent", Concurrent);
            writer.WritePropertyName("pause_durations");
            writer.WriteStartArray();
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
