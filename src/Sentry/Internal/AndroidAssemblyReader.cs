// TODO this is actually only needed for Android but we want to test on local devices. Is there another way to do this?
#if NET6_0_OR_GREATER

using System.IO.Compression;
using System.Reflection.PortableExecutable;
using Sentry.Extensibility;
using K4os.Compression.LZ4;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Sentry.Internal;

internal interface IAndroidAssemblyReader : IDisposable
{
    PEReader? TryReadAssembly(string name);
}

internal sealed class AndroidAssemblyReaderFactory
{
    public static IAndroidAssemblyReader Open(string apkPath, IList<string> supportedAbis, IDiagnosticLogger? logger)
    {
        logger?.LogDebug("Opening APK: {0}", apkPath);
        var zipArchive = ZipFile.Open(apkPath, ZipArchiveMode.Read);

        if (zipArchive.GetEntry("assemblies/assemblies.manifest") is not null)
        {
            logger?.LogDebug("APK uses AssemblyStore");
            return new AndroidAssemblyStoreReader(zipArchive, supportedAbis, logger);
        }
        else
        {
            logger?.LogDebug("APK doesn't use AssemblyStore");
            return new AndroidAssemblyDirectoryReader(zipArchive, supportedAbis, logger);
        }
    }
}

internal class AndroidAssemblyReader : IDisposable
{
    protected readonly IDiagnosticLogger? _logger;
    protected readonly ZipArchive _zipArchive;
    protected readonly IList<string> _supportedAbis;

    public AndroidAssemblyReader(ZipArchive zip, IList<string> supportedAbis, IDiagnosticLogger? logger)
    {
        _zipArchive = zip;
        _logger = logger;
        _supportedAbis = supportedAbis;
    }

    public void Dispose()
    {
        _zipArchive.Dispose();
    }

    protected PEReader CreatePEReader(string assemblyName, MemoryStream inputStream)
    {
        var decompressedStream = TryDecompressLZ4(assemblyName, inputStream);
        // Use the decompressed stream, or if null, i.e. it wasn't compressed, use the original.
        return new(decompressedStream ?? inputStream);
    }

    /// <summary>
    /// The DLL may be LZ4 compressed, see https://github.com/xamarin/xamarin-android/pull/4686
    /// The format is:
    ///    [ 4 byte magic header ] (XALZ)
    ///    [ 4 byte header index ]
    ///    [ 4 byte uncompressed payload length ]
    ///    [rest: lz4 compressed payload]
    /// </summary>
    /// <seealso href="https://github.com/xamarin/xamarin-android/blob/c92702619f5fabcff0ed88e09160baf9edd70f41/tools/decompress-assemblies/main.cs#L26" />
    private Stream? TryDecompressLZ4(string assemblyName, MemoryStream inputStream)
    {
        const uint CompressedDataMagic = 0x5A4C4158; // 'XALZ', little-endian
        const int payloadOffset = 12;
        var reader = new BinaryReader(inputStream);
        if (reader.ReadUInt32() != CompressedDataMagic)
        {
            // Restore the input stream to the begininng if we're not decompressing.
            inputStream.Position = 0;
            return null;
        }
        reader.ReadUInt32(); // ignore descriptor index, we don't need it
        var decompressedLength = reader.ReadInt32();
        Debug.Assert(inputStream.Position == payloadOffset);
        var inputLength = (int)(inputStream.Length - payloadOffset);

        _logger?.LogDebug("Decompressing assembly ({0} bytes uncompressed) using LZ4", decompressedLength);

        var outputStream = new MemoryStream(decompressedLength);

        // We're writing to the underlying array manually, so we need to set the length.
        outputStream.SetLength(decompressedLength);
        var outputBuffer = outputStream.GetBuffer();

        var inputBuffer = inputStream is MemorySlice ? (inputStream as MemorySlice)!.FullBuffer : inputStream.GetBuffer();
        var offset = inputStream is MemorySlice ? (inputStream as MemorySlice)!.Offset + payloadOffset : payloadOffset;
        var decoded = LZ4Codec.Decode(inputBuffer, offset, inputLength, outputBuffer, 0, decompressedLength);
        if (decoded != decompressedLength)
        {
            throw new Exception($"Failed to decompress LZ4 data of assembly {assemblyName} - decoded {decoded} instead of expected {decompressedLength} bytes");
        }
        return outputStream;
    }

    // Allows consumer to access the underlying buffer even if the MemoryStream is created as a slice over another.
    // Plain MemoryStream would throw "MemoryStream's internal buffer cannot be accessed."
    protected class MemorySlice : MemoryStream
    {
        public readonly int Offset;
        public readonly byte[] FullBuffer;

        public MemorySlice(MemoryStream other, int offset, int size) : base(other.GetBuffer(), offset, size, writable: false)
        {
            Offset = offset;
            FullBuffer = other.GetBuffer();
        }
    }
}

// The "Old" app type - where each DLL is placed in the 'assemblies' directory as an individual file.
internal sealed class AndroidAssemblyDirectoryReader : AndroidAssemblyReader, IAndroidAssemblyReader
{
    public AndroidAssemblyDirectoryReader(ZipArchive zip, IList<string> supportedAbis, IDiagnosticLogger? logger)
        : base(zip, supportedAbis, logger) { }

    public PEReader? TryReadAssembly(string name)
    {
        var zipEntry = FindAssembly(name);
        if (zipEntry is null)
        {
            _logger?.LogDebug("Couldn't find assembly {0} in the APK", name);
            return null;
        }

        _logger?.LogDebug("Resolved assembly {0} in the APK at {1}", name, zipEntry.FullName);

        // We need a seekable stream for the PEReader (or even to check whether the DLL is compressed), so make a copy.
        var memStream = new MemoryStream((int)zipEntry.Length);
        using (var zipStream = zipEntry.Open())
        {
            zipStream.CopyTo(memStream);
            memStream.Position = 0;
        }
        return CreatePEReader(name, memStream);
    }

    private ZipArchiveEntry? FindAssembly(string name)
    {
        var zipEntry = _zipArchive.GetEntry($"assemblies/{name}");

        if (zipEntry is null)
        {
            foreach (var abi in _supportedAbis)
            {
                if (abi.Length > 0)
                {
                    zipEntry = _zipArchive.GetEntry($"assemblies/{abi}/{name}");
                    if (zipEntry is not null)
                    {
                        break;
                    }
                }
            }
        }

        return zipEntry;
    }
}

// See https://devblogs.microsoft.com/dotnet/performance-improvements-in-dotnet-maui/#single-file-assembly-stores
internal sealed class AndroidAssemblyStoreReader : AndroidAssemblyReader, IAndroidAssemblyReader
{
    private readonly AssemblyStoreExplorer _explorer;

    public AndroidAssemblyStoreReader(ZipArchive zip, IList<string> supportedAbis, IDiagnosticLogger? logger)
        : base(zip, supportedAbis, logger)
    {
        _explorer = new(zip, supportedAbis, logger);
    }

    public PEReader? TryReadAssembly(string name)
    {
        var assembly = TryFindAssembly(name);
        if (assembly is null)
        {
            _logger?.LogDebug("Couldn't find assembly {0} in the APK AssemblyStore", name);
            return null;
        }

        _logger?.LogDebug("Resolved assembly {0} in the APK {1} AssemblyStore", name, assembly.Store.Arch);

        var stream = assembly.GetImage();
        if (stream is null)
        {
            _logger?.LogDebug("Couldn't access assembly {0} image stream", name);
            return null;
        }

        return CreatePEReader(name, stream);
    }

    private AssemblyStoreAssembly? TryFindAssembly(string name)
    {
        if (_explorer.AssembliesByName.TryGetValue(name, out var assembly))
        {
            return assembly;
        }

        if (name.EndsWith(".dll", ignoreCase: true, CultureInfo.InvariantCulture) ||
            name.EndsWith(".exe", ignoreCase: true, CultureInfo.InvariantCulture))
        {
            if (_explorer.AssembliesByName.TryGetValue(name.Substring(0, name.Length - 4), out assembly))
            {
                return assembly;
            }
        }

        return null;
    }

    // Adapted from https://github.com/xamarin/xamarin-android/blob/c92702619f5fabcff0ed88e09160baf9edd70f41/tools/assembly-store-reader/AssemblyStoreExplorer.cs
    // With the original code licensed under MIT License (https://github.com/xamarin/xamarin-android/blob/2bd13c4a00ae78db34663a4b9c7a4c5bfb20c344/LICENSE).
    private class AssemblyStoreExplorer
    {
        private AssemblyStoreReader? _indexStore;
        private readonly AssemblyStoreManifestReader _manifest;
        private readonly IDiagnosticLogger? _logger;
        public IDictionary<string, AssemblyStoreAssembly> AssembliesByName { get; } = new SortedDictionary<string, AssemblyStoreAssembly>(StringComparer.OrdinalIgnoreCase);
        public IDictionary<uint, AssemblyStoreAssembly> AssembliesByHash32 { get; } = new Dictionary<uint, AssemblyStoreAssembly>();
        public IDictionary<ulong, AssemblyStoreAssembly> AssembliesByHash64 { get; } = new Dictionary<ulong, AssemblyStoreAssembly>();
        public List<AssemblyStoreAssembly> Assemblies { get; } = new List<AssemblyStoreAssembly>();
        public IDictionary<uint, List<AssemblyStoreReader>> Stores { get; } = new SortedDictionary<uint, List<AssemblyStoreReader>>();

        public AssemblyStoreExplorer(ZipArchive zip, IList<string> supportedAbis, IDiagnosticLogger? logger)
        {
            _logger = logger;
            _manifest = new AssemblyStoreManifestReader(zip.GetEntry("assemblies/assemblies.manifest")!.Open());

            TryAddStore(zip, null);
            foreach (var abi in supportedAbis)
            {
                if (!string.IsNullOrEmpty(abi))
                {
                    TryAddStore(zip, abi);
                }
            }
            zip.Dispose();
            ProcessStores();
        }

        private void ProcessStores()
        {
            if (Stores.Count == 0 || _indexStore == null)
            {
                return;
            }

            ProcessIndex(_indexStore.GlobalIndex32, "32", (AssemblyStoreHashEntry he, AssemblyStoreAssembly assembly) =>
            {
                assembly.Hash32 = (uint)he.Hash;
                assembly.RuntimeIndex = he.MappingIndex;

                if (_manifest != null && _manifest.EntriesByHash32.TryGetValue(assembly.Hash32, out var me) && me != null)
                {
                    assembly.Name = me.Name;
                }

                if (!AssembliesByHash32.ContainsKey(assembly.Hash32))
                {
                    AssembliesByHash32.Add(assembly.Hash32, assembly);
                }
            });

            ProcessIndex(_indexStore.GlobalIndex64, "64", (AssemblyStoreHashEntry he, AssemblyStoreAssembly assembly) =>
            {
                assembly.Hash64 = he.Hash;
                if (assembly.RuntimeIndex != he.MappingIndex)
                {
                    _logger?.LogDebug($"assembly with hashes 0x{assembly.Hash32} and 0x{assembly.Hash64} has a different 32-bit runtime index ({assembly.RuntimeIndex}) than the 64-bit runtime index({he.MappingIndex})");
                }

                if (_manifest != null && _manifest.EntriesByHash64.TryGetValue(assembly.Hash64, out var me) && me != null)
                {
                    if (string.IsNullOrEmpty(assembly.Name))
                    {
                        _logger?.LogDebug($"32-bit hash 0x{assembly.Hash32:x} did not match any assembly name in the manifest");
                        assembly.Name = me.Name;
                        if (string.IsNullOrEmpty(assembly.Name))
                        {
                            _logger?.LogDebug($"64-bit hash 0x{assembly.Hash64:x} did not match any assembly name in the manifest");
                        }
                    }
                    else if (!string.Equals(assembly.Name, me.Name, StringComparison.Ordinal))
                    {
                        _logger?.LogDebug($"32-bit hash 0x{assembly.Hash32:x} maps to assembly name '{assembly.Name}', however 64-bit hash 0x{assembly.Hash64:x} for the same entry matches assembly name '{me.Name}'");
                    }
                }

                if (!AssembliesByHash64.ContainsKey(assembly.Hash64))
                {
                    AssembliesByHash64.Add(assembly.Hash64, assembly);
                }
            });

            foreach (var kvp in Stores)
            {
                List<AssemblyStoreReader> list = kvp.Value;
                if (list.Count < 2)
                {
                    continue;
                }

                AssemblyStoreReader template = list[0];
                for (int i = 1; i < list.Count; i++)
                {
                    AssemblyStoreReader other = list[i];
                    if (!template.HasIdenticalContent(other))
                    {
                        throw new Exception($"Store ID {template.StoreID} for architecture {other.Arch} is not identical to other stores with the same ID");
                    }
                }
            }

            void ProcessIndex(List<AssemblyStoreHashEntry> index, string bitness, Action<AssemblyStoreHashEntry, AssemblyStoreAssembly> assemblyHandler)
            {
                foreach (AssemblyStoreHashEntry he in index)
                {
                    if (!Stores.TryGetValue(he.StoreID, out List<AssemblyStoreReader>? storeList) || storeList == null)
                    {
                        _logger?.LogDebug($"store with id {he.StoreID} not part of the set");
                        continue;
                    }

                    foreach (AssemblyStoreReader store in storeList)
                    {
                        if (he.LocalStoreIndex >= (uint)store.Assemblies.Count)
                        {
                            _logger?.LogDebug($"{bitness}-bit index entry with hash 0x{he.Hash:x} has invalid store {store.StoreID} index {he.LocalStoreIndex} (maximum allowed is {store.Assemblies.Count})");
                            continue;
                        }

                        AssemblyStoreAssembly assembly = store.Assemblies[(int)he.LocalStoreIndex];
                        assemblyHandler(he, assembly);

                        if (!AssembliesByName.ContainsKey(assembly.Name))
                        {
                            AssembliesByName.Add(assembly.Name, assembly);
                        }
                    }
                }
            }
        }

        private void TryAddStore(ZipArchive archive, string? abi)
        {
            var infix = abi is null ? "" : $".{abi}";
            if (archive.GetEntry($"assemblies/assemblies{infix}.blob") is { } zipEntry)
            {
                var memStream = new MemoryStream((int)zipEntry.Length);
                using (var zipStream = zipEntry.Open())
                {
                    zipStream.CopyTo(memStream);
                    memStream.Position = 0;
                }
                AddStore(new AssemblyStoreReader(memStream, abi));
            }
        }

        private void AddStore(AssemblyStoreReader reader)
        {
            if (reader.HasGlobalIndex)
            {
                _indexStore = reader;
            }

            if (!Stores.TryGetValue(reader.StoreID, out var storeList))
            {
                storeList = new List<AssemblyStoreReader>();
                Stores.Add(reader.StoreID, storeList);
            }
            storeList.Add(reader);

            Assemblies.AddRange(reader.Assemblies);
        }
    }

    // Adapted from https://github.com/xamarin/xamarin-android/blob/c92702619f5fabcff0ed88e09160baf9edd70f41/tools/assembly-store-reader/AssemblyStoreManifestReader.cs
    // With the original code licensed under MIT License (https://github.com/xamarin/xamarin-android/blob/2bd13c4a00ae78db34663a4b9c7a4c5bfb20c344/LICENSE).
    private class AssemblyStoreManifestReader
    {
        public List<AssemblyStoreManifestEntry> Entries { get; } = new List<AssemblyStoreManifestEntry>();
        public Dictionary<uint, AssemblyStoreManifestEntry> EntriesByHash32 { get; } = new Dictionary<uint, AssemblyStoreManifestEntry>();
        public Dictionary<ulong, AssemblyStoreManifestEntry> EntriesByHash64 { get; } = new Dictionary<ulong, AssemblyStoreManifestEntry>();

        public AssemblyStoreManifestReader(Stream manifest)
        {
            using var sr = new StreamReader(manifest, Encoding.UTF8, detectEncodingFromByteOrderMarks: false);
            ReadManifest(sr);
        }

        private void ReadManifest(StreamReader reader)
        {
            // First line is ignored, it contains headers
            reader.ReadLine();

            // Each subsequent line consists of fields separated with any number of spaces (for the pleasure of a human being reading the manifest)
            while (!reader.EndOfStream)
            {
                string[]? fields = reader.ReadLine()?.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (fields == null)
                {
                    continue;
                }

                var entry = new AssemblyStoreManifestEntry(fields);
                Entries.Add(entry);
                if (entry.Hash32 != 0)
                {
                    EntriesByHash32.Add(entry.Hash32, entry);
                }

                if (entry.Hash64 != 0)
                {
                    EntriesByHash64.Add(entry.Hash64, entry);
                }
            }
        }
        // public readonly Dictionary<string, AssemblyStoreManifestEntry?> EntriesByName = new();

        // public AssemblyStoreManifestReader(Stream manifest)
        // {
        //     using (var sr = new StreamReader(manifest, Encoding.UTF8, detectEncodingFromByteOrderMarks: false))
        //     {
        //         ReadManifest(sr);
        //     }
        // }

        // void ReadManifest(StreamReader reader)
        // {
        //     // First line is ignored, it contains headers
        //     reader.ReadLine();

        //     // Each subsequent line consists of fields separated with any number of spaces (for the pleasure of a human being reading the manifest)
        //     while (!reader.EndOfStream)
        //     {
        //         string[]? fields = reader.ReadLine()?.Split(' ', stringSplitOptions.RemoveEmptyEntries);
        //         if (fields == null)
        //         {
        //             continue;
        //         }

        //         var entry = new AssemblyStoreManifestEntry(fields);
        //         if (EntriesByName.ContainsKey(entry.Name))
        //         {
        //             // Duplicate entries - not sure it's possible, but let's be safe.
        //             EntriesByName[entry.Name] = null;
        //         }
        //         else
        //         {
        //             EntriesByName.Add(entry.Name, entry);
        //         }
        //     }
        // }
    }

    // Adapted from https://github.com/xamarin/xamarin-android/blob/c92702619f5fabcff0ed88e09160baf9edd70f41/tools/assembly-store-reader/AssemblyStoreManifestEntry.cs
    // With the original code licensed under MIT License (https://github.com/xamarin/xamarin-android/blob/2bd13c4a00ae78db34663a4b9c7a4c5bfb20c344/LICENSE).
    private class AssemblyStoreManifestEntry
    {
        // Fields are:
        //  Hash 32 | Hash 64 | Store ID | Store idx | Name
        private const int NumberOfFields = 5;
        private const int Hash32FieldIndex = 0;
        private const int Hash64FieldIndex = 1;
        private const int StoreIDFieldIndex = 2;
        private const int StoreIndexFieldIndex = 3;
        private const int NameFieldIndex = 4;

        public uint Hash32 { get; }
        public ulong Hash64 { get; }
        public uint StoreID { get; }
        public uint IndexInStore { get; }
        public string Name { get; }

        public AssemblyStoreManifestEntry(string[] fields)
        {
            if (fields.Length != NumberOfFields)
            {
                throw new ArgumentOutOfRangeException(nameof(fields), "Invalid number of fields");
            }

            Hash32 = GetUInt32(fields[Hash32FieldIndex]);
            Hash64 = GetUInt64(fields[Hash64FieldIndex]);
            StoreID = GetUInt32(fields[StoreIDFieldIndex]);
            IndexInStore = GetUInt32(fields[StoreIndexFieldIndex]);
            Name = fields[NameFieldIndex].Trim();
        }

        private static uint GetUInt32(string value)
        {
            if (uint.TryParse(PrepHexValue(value), NumberStyles.HexNumber, null, out uint hash))
            {
                return hash;
            }

            return 0;
        }

        private static ulong GetUInt64(string value)
        {
            if (ulong.TryParse(PrepHexValue(value), NumberStyles.HexNumber, null, out ulong hash))
            {
                return hash;
            }

            return 0;
        }

        private static string PrepHexValue(string value)
        {
            if (value.StartsWith("0x", StringComparison.Ordinal))
            {
                return value.Substring(2);
            }

            return value;
        }
    }

    // Adapted from https://github.com/xamarin/xamarin-android/blob/c92702619f5fabcff0ed88e09160baf9edd70f41/tools/assembly-store-reader/AssemblyStoreHashEntry.cs
    // With the original code licensed under MIT License (https://github.com/xamarin/xamarin-android/blob/2bd13c4a00ae78db34663a4b9c7a4c5bfb20c344/LICENSE).
    private class AssemblyStoreHashEntry
    {
        public bool Is32Bit { get; }

        public ulong Hash { get; }
        public uint MappingIndex { get; }
        public uint LocalStoreIndex { get; }
        public uint StoreID { get; }

        internal AssemblyStoreHashEntry(BinaryReader reader, bool is32Bit)
        {
            Is32Bit = is32Bit;

            Hash = reader.ReadUInt64();
            MappingIndex = reader.ReadUInt32();
            LocalStoreIndex = reader.ReadUInt32();
            StoreID = reader.ReadUInt32();
        }
    }

    // Adapted from https://github.com/xamarin/xamarin-android/blob/c92702619f5fabcff0ed88e09160baf9edd70f41/tools/assembly-store-reader/AssemblyStoreReader.cs
    // With the original code licensed under MIT License (https://github.com/xamarin/xamarin-android/blob/2bd13c4a00ae78db34663a4b9c7a4c5bfb20c344/LICENSE).
    private class AssemblyStoreReader
    {
        // These two constants must be identical to the native ones in src/monodroid/jni/xamarin-app.hh
        private const uint ASSEMBLY_STORE_MAGIC = 0x41424158; // 'XABA', little-endian
        private const uint ASSEMBLY_STORE_FORMAT_VERSION = 1; // The highest format version this reader understands

        private readonly MemoryStream _storeData;

        public uint Version { get; private set; }
        public uint LocalEntryCount { get; private set; }
        public uint GlobalEntryCount { get; private set; }
        public uint StoreID { get; private set; }
        public List<AssemblyStoreAssembly> Assemblies { get; }
        public List<AssemblyStoreHashEntry> GlobalIndex32 { get; } = new List<AssemblyStoreHashEntry>();
        public List<AssemblyStoreHashEntry> GlobalIndex64 { get; } = new List<AssemblyStoreHashEntry>();
        public string Arch { get; }

        public bool HasGlobalIndex => StoreID == 0;

        public AssemblyStoreReader(MemoryStream store, string? arch = null)
        {
            Arch = arch ?? string.Empty;
            _storeData = store;
            using var reader = new BinaryReader(store, Encoding.UTF8, leaveOpen: true);
            ReadHeader(reader);

            Assemblies = new List<AssemblyStoreAssembly>();
            ReadLocalEntries(reader, Assemblies);
            if (HasGlobalIndex)
            {
                ReadGlobalIndex(reader, GlobalIndex32, GlobalIndex64);
            }
        }

        internal MemoryStream? GetAssemblyImageSlice(AssemblyStoreAssembly assembly)
        {
            return GetDataSlice(assembly.DataOffset, assembly.DataSize);
        }

        internal MemoryStream? GetAssemblyDebugDataSlice(AssemblyStoreAssembly assembly)
        {
            if (assembly.DebugDataOffset == 0 || assembly.DebugDataSize == 0)
            {
                return null;
            }
            return GetDataSlice(assembly.DebugDataOffset, assembly.DebugDataSize);
        }

        internal MemoryStream? GetAssemblyConfigSlice(AssemblyStoreAssembly assembly)
        {
            if (assembly.ConfigDataOffset == 0 || assembly.ConfigDataSize == 0)
            {
                return null;
            }
            return GetDataSlice(assembly.ConfigDataOffset, assembly.ConfigDataSize);
        }

        private MemoryStream GetDataSlice(uint offset, uint size) => new MemorySlice(_storeData, (int)offset, (int)size);

        public bool HasIdenticalContent(AssemblyStoreReader other)
        {
            return
                other.Version == Version &&
                other.LocalEntryCount == LocalEntryCount &&
                other.GlobalEntryCount == GlobalEntryCount &&
                other.StoreID == StoreID &&
                other.Assemblies.Count == Assemblies.Count &&
                other.GlobalIndex32.Count == GlobalIndex32.Count &&
                other.GlobalIndex64.Count == GlobalIndex64.Count;
        }

        private void ReadHeader(BinaryReader reader)
        {
            if (reader.ReadUInt32() != ASSEMBLY_STORE_MAGIC)
            {
                throw new InvalidOperationException("Invalid header magic number");
            }

            Version = reader.ReadUInt32();
            if (Version == 0)
            {
                throw new InvalidOperationException("Invalid version number: 0");
            }

            if (Version > ASSEMBLY_STORE_FORMAT_VERSION)
            {
                throw new InvalidOperationException($"Store format version {Version} is higher than the one understood by this reader, {ASSEMBLY_STORE_FORMAT_VERSION}");
            }

            LocalEntryCount = reader.ReadUInt32();
            GlobalEntryCount = reader.ReadUInt32();
            StoreID = reader.ReadUInt32();
        }

        private void ReadLocalEntries(BinaryReader reader, List<AssemblyStoreAssembly> assemblies)
        {
            for (uint i = 0; i < LocalEntryCount; i++)
            {
                assemblies.Add(new AssemblyStoreAssembly(reader, this));
            }
        }

        private void ReadGlobalIndex(BinaryReader reader, List<AssemblyStoreHashEntry> index32, List<AssemblyStoreHashEntry> index64)
        {
            ReadIndex(true, index32);
            ReadIndex(false, index64);

            void ReadIndex(bool is32Bit, List<AssemblyStoreHashEntry> index)
            {
                for (uint i = 0; i < GlobalEntryCount; i++)
                {
                    index.Add(new AssemblyStoreHashEntry(reader, is32Bit));
                }
            }
        }
    }

    // Adapted from https://github.com/xamarin/xamarin-android/blob/c92702619f5fabcff0ed88e09160baf9edd70f41/tools/assembly-store-reader/AssemblyStoreAssembly.cs
    // With the original code licensed under MIT License (https://github.com/xamarin/xamarin-android/blob/2bd13c4a00ae78db34663a4b9c7a4c5bfb20c344/LICENSE).
    private class AssemblyStoreAssembly
    {
        public uint DataOffset { get; }
        public uint DataSize { get; }
        public uint DebugDataOffset { get; }
        public uint DebugDataSize { get; }
        public uint ConfigDataOffset { get; }
        public uint ConfigDataSize { get; }

        public uint Hash32 { get; set; }
        public ulong Hash64 { get; set; }
        public string Name { get; set; } = string.Empty;
        public uint RuntimeIndex { get; set; }

        public AssemblyStoreReader Store { get; }

        internal AssemblyStoreAssembly(BinaryReader reader, AssemblyStoreReader store)
        {
            Store = store;

            DataOffset = reader.ReadUInt32();
            DataSize = reader.ReadUInt32();
            DebugDataOffset = reader.ReadUInt32();
            DebugDataSize = reader.ReadUInt32();
            ConfigDataOffset = reader.ReadUInt32();
            ConfigDataSize = reader.ReadUInt32();
        }

        public MemoryStream? GetImage() => Store.GetAssemblyImageSlice(this);

        public MemoryStream? GetDebugData() => Store.GetAssemblyDebugDataSlice(this);

        public MemoryStream? GetAssemblyConfig() => Store.GetAssemblyConfigSlice(this);
    }
}
#endif
