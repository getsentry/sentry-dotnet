/*
 * Adapted from https://github.com/dotnet/android/blob/5ebcb1dd1503648391e3c0548200495f634d90c6/tools/assembly-store-reader-mk2/AssemblyStore/StoreReader_V2.Classes.cs
 * Original code licensed under the MIT License (https://github.com/dotnet/android/blob/5ebcb1dd1503648391e3c0548200495f634d90c6/LICENSE.TXT)
 */

namespace Sentry.Android.AssemblyReader.V2;

internal partial class StoreReaderV2
{
    private sealed class Header
    {
        public const uint NativeSize = 5 * sizeof(uint);

        public readonly uint magic;
        public readonly uint version;
        public readonly uint entry_count;
        public readonly uint index_entry_count;

        // Index size in bytes
        public readonly uint index_size;

        public Header(uint magic, uint version, uint entry_count, uint index_entry_count, uint index_size)
        {
            this.magic = magic;
            this.version = version;
            this.entry_count = entry_count;
            this.index_entry_count = index_entry_count;
            this.index_size = index_size;
        }
    }

    private sealed class IndexEntry
    {
        public readonly ulong name_hash;
        public readonly uint descriptor_index;

        public IndexEntry(ulong name_hash, uint descriptor_index)
        {
            this.name_hash = name_hash;
            this.descriptor_index = descriptor_index;
        }
    }

    private sealed class EntryDescriptor
    {
        public uint mapping_index;

        public uint data_offset;
        public uint data_size;

        public uint debug_data_offset;
        public uint debug_data_size;

        public uint config_data_offset;
        public uint config_data_size;
    }

    private sealed class StoreItemV2 : AssemblyStoreItem
    {
        public StoreItemV2(AndroidTargetArch targetArch, string name, bool is64Bit, List<IndexEntry> indexEntries, EntryDescriptor descriptor)
            : base(name, is64Bit, IndexToHashes(indexEntries))
        {
            DataOffset = descriptor.data_offset;
            DataSize = descriptor.data_size;
            DebugOffset = descriptor.debug_data_offset;
            DebugSize = descriptor.debug_data_size;
            ConfigOffset = descriptor.config_data_offset;
            ConfigSize = descriptor.config_data_size;
            TargetArch = targetArch;
        }

        private static List<ulong> IndexToHashes(List<IndexEntry> indexEntries)
        {
            var ret = new List<ulong>();
            foreach (var ie in indexEntries)
            {
                ret.Add(ie.name_hash);
            }

            return ret;
        }
    }

    private sealed class TemporaryItem
    {
        public readonly string Name;
        public readonly List<IndexEntry> IndexEntries = new List<IndexEntry>();
        public readonly EntryDescriptor Descriptor;

        public TemporaryItem(string name, EntryDescriptor descriptor)
        {
            Name = name;
            Descriptor = descriptor;
        }
    }
}
