/*
 * Adapted from https://github.com/dotnet/android/blob/5ebcb1dd1503648391e3c0548200495f634d90c6/tools/assembly-store-reader-mk2/AssemblyStore/StoreReader_V2.cs
 * Original code licensed under the MIT License (https://github.com/dotnet/android/blob/5ebcb1dd1503648391e3c0548200495f634d90c6/LICENSE.TXT)
 */

namespace Sentry.Android.AssemblyReader.V2;

internal partial class StoreReaderV2 : AssemblyStoreReader
{
    // Bit 31 is set for 64-bit platforms, cleared for the 32-bit ones
    private const uint ASSEMBLY_STORE_FORMAT_VERSION_64BIT = 0x80000002; // Must match the ASSEMBLY_STORE_FORMAT_VERSION native constant
    private const uint ASSEMBLY_STORE_FORMAT_VERSION_32BIT = 0x00000002;
    private const uint ASSEMBLY_STORE_FORMAT_VERSION_MASK = 0xF0000000;
    private const uint ASSEMBLY_STORE_ABI_AARCH64 = 0x00010000;
    private const uint ASSEMBLY_STORE_ABI_ARM = 0x00020000;
    private const uint ASSEMBLY_STORE_ABI_X64 = 0x00030000;
    private const uint ASSEMBLY_STORE_ABI_X86 = 0x00040000;
    private const uint ASSEMBLY_STORE_ABI_MASK = 0x00FF0000;

    public override string Description => "Assembly store v2";
    public override bool NeedsExtensionInName => true;

    public static IList<string> ApkPaths { get; }
    public static IList<string> AabPaths { get; }
    public static IList<string> AabBasePaths { get; }

    private readonly HashSet<uint> supportedVersions;
    private Header? header;
    private ulong elfOffset = 0;

    static StoreReaderV2()
    {
        var paths = new List<string> {
            GetArchPath (AndroidTargetArch.Arm64),
            GetArchPath (AndroidTargetArch.Arm),
            GetArchPath (AndroidTargetArch.X86_64),
            GetArchPath (AndroidTargetArch.X86),
        };
        ApkPaths = paths.AsReadOnly();
        AabBasePaths = ApkPaths;

        const string AabBaseDir = "base";
        paths = new List<string> {
            GetArchPath (AndroidTargetArch.Arm64, AabBaseDir),
            GetArchPath (AndroidTargetArch.Arm, AabBaseDir),
            GetArchPath (AndroidTargetArch.X86_64, AabBaseDir),
            GetArchPath (AndroidTargetArch.X86, AabBaseDir),
        };
        AabPaths = paths.AsReadOnly();

        string GetArchPath(AndroidTargetArch arch, string? root = null)
        {
            const string LibDirName = "lib";

            string abi = MonoAndroidHelper.ArchToAbi(arch);
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(root))
            {
                parts.Add(LibDirName);
            }
            else
            {
                root = LibDirName;
            }
            parts.Add(abi);
            parts.Add(GetBlobName(abi));

            return MonoAndroidHelper.MakeZipArchivePath(root, parts);
        }
    }

    public StoreReaderV2(Stream store, string path, DebugLogger? logger)
        : base(store, path, logger)
    {
        supportedVersions = new HashSet<uint> {
            ASSEMBLY_STORE_FORMAT_VERSION_64BIT | ASSEMBLY_STORE_ABI_AARCH64,
            ASSEMBLY_STORE_FORMAT_VERSION_64BIT | ASSEMBLY_STORE_ABI_X64,
            ASSEMBLY_STORE_FORMAT_VERSION_32BIT | ASSEMBLY_STORE_ABI_ARM,
            ASSEMBLY_STORE_FORMAT_VERSION_32BIT | ASSEMBLY_STORE_ABI_X86,
        };
    }

    private static string GetBlobName(string abi) => $"libassemblies.{abi}.blob.so";

    protected override ulong GetStoreStartDataOffset() => elfOffset;

    protected override bool IsSupported()
    {
        StoreStream.Seek(0, SeekOrigin.Begin);
        using var reader = CreateReader();

        uint magic = reader.ReadUInt32();
        if (magic == Utils.ELFMagic)
        {
            ELFPayloadError error;
            (elfOffset, _, error) = Utils.FindELFPayloadSectionOffsetAndSize(StoreStream);

            if (error != ELFPayloadError.None)
            {
                string message = error switch
                {
                    ELFPayloadError.NotELF => $"Store '{StorePath}' is not a valid ELF binary",
                    ELFPayloadError.LoadFailed => $"Store '{StorePath}' could not be loaded",
                    ELFPayloadError.NotSharedLibrary => $"Store '{StorePath}' is not a shared ELF library",
                    ELFPayloadError.NotLittleEndian => $"Store '{StorePath}' is not a little-endian ELF image",
                    ELFPayloadError.NoPayloadSection => $"Store '{StorePath}' does not contain the 'payload' section",
                    _ => $"Unknown ELF payload section error for store '{StorePath}': {error}"
                };
                Logger?.Invoke(message);
                // Was originally:
                // ```
                // } else if (elfOffset >= 0) {
                // ```
                // However since elfOffset is an ulong, it will never be less than 0
            }
            else
            {
                StoreStream.Seek((long)elfOffset, SeekOrigin.Begin);
                magic = reader.ReadUInt32();
            }
        }

        if (magic != Utils.AssemblyStoreMagic)
        {
            Logger?.Invoke("Store '{0}' has invalid header magic number.", StorePath);
            return false;
        }

        uint version = reader.ReadUInt32();
        if (!supportedVersions.Contains(version))
        {
            Logger?.Invoke("Store '{0}' has unsupported version 0x{1:x}", StorePath, version);
            return false;
        }

        uint entry_count = reader.ReadUInt32();
        uint index_entry_count = reader.ReadUInt32();
        uint index_size = reader.ReadUInt32();

        header = new Header(magic, version, entry_count, index_entry_count, index_size);
        return true;
    }

    protected override void Prepare()
    {
        if (header == null)
        {
            throw new InvalidOperationException("Internal error: header not set, was IsSupported() called?");
        }

        TargetArch = (header.version & ASSEMBLY_STORE_ABI_MASK) switch
        {
            ASSEMBLY_STORE_ABI_AARCH64 => AndroidTargetArch.Arm64,
            ASSEMBLY_STORE_ABI_ARM => AndroidTargetArch.Arm,
            ASSEMBLY_STORE_ABI_X64 => AndroidTargetArch.X86_64,
            ASSEMBLY_STORE_ABI_X86 => AndroidTargetArch.X86,
            _ => throw new NotSupportedException($"Unsupported ABI in store version: 0x{header.version:x}")
        };

        Is64Bit = (header.version & ASSEMBLY_STORE_FORMAT_VERSION_MASK) != 0;
        AssemblyCount = header.entry_count;
        IndexEntryCount = header.index_entry_count;

        StoreStream.Seek((long)elfOffset + Header.NativeSize, SeekOrigin.Begin);
        using var reader = CreateReader();

        var index = new List<IndexEntry>();
        for (uint i = 0; i < header.index_entry_count; i++)
        {
            ulong name_hash;
            if (Is64Bit)
            {
                name_hash = reader.ReadUInt64();
            }
            else
            {
                name_hash = (ulong)reader.ReadUInt32();
            }

            uint descriptor_index = reader.ReadUInt32();
            index.Add(new IndexEntry(name_hash, descriptor_index));
        }

        var descriptors = new List<EntryDescriptor>();
        for (uint i = 0; i < header.entry_count; i++)
        {
            uint mapping_index = reader.ReadUInt32();
            uint data_offset = reader.ReadUInt32();
            uint data_size = reader.ReadUInt32();
            uint debug_data_offset = reader.ReadUInt32();
            uint debug_data_size = reader.ReadUInt32();
            uint config_data_offset = reader.ReadUInt32();
            uint config_data_size = reader.ReadUInt32();

            var desc = new EntryDescriptor
            {
                mapping_index = mapping_index,
                data_offset = data_offset,
                data_size = data_size,
                debug_data_offset = debug_data_offset,
                debug_data_size = debug_data_size,
                config_data_offset = config_data_offset,
                config_data_size = config_data_size,
            };
            descriptors.Add(desc);
        }

        var names = new List<string>();
        for (uint i = 0; i < header.entry_count; i++)
        {
            uint name_length = reader.ReadUInt32();
            byte[] name_bytes = reader.ReadBytes((int)name_length);
            names.Add(Encoding.UTF8.GetString(name_bytes));
        }

        var tempItems = new Dictionary<uint, TemporaryItem>();
        foreach (IndexEntry ie in index)
        {
            if (!tempItems.TryGetValue(ie.descriptor_index, out TemporaryItem? item))
            {
                item = new TemporaryItem(names[(int)ie.descriptor_index], descriptors[(int)ie.descriptor_index]);
                tempItems.Add(ie.descriptor_index, item);
            }
            item.IndexEntries.Add(ie);
        }

        if (tempItems.Count != descriptors.Count)
        {
            throw new InvalidOperationException($"Assembly store '{StorePath}' index is corrupted.");
        }

        var storeItems = new List<AssemblyStoreItem>();
        foreach (var kvp in tempItems)
        {
            TemporaryItem ti = kvp.Value;
            var item = new StoreItemV2(TargetArch, ti.Name, Is64Bit, ti.IndexEntries, ti.Descriptor);
            storeItems.Add(item);
        }
        Assemblies = storeItems.AsReadOnly();
    }
}
