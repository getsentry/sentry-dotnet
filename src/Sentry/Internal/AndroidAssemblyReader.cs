using System.IO.Compression;
using System.Reflection.PortableExecutable;
using Sentry.Extensibility;
using K4os.Compression.LZ4;
using System.Diagnostics;
using System.Buffers;

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
        // Note: We open in Update mode so that the streams returned by ZipArchiveEntry.Open() support seeking.
        // We don't actually do any updates though.
        var zipArchive = ZipFile.Open(apkPath, ZipArchiveMode.Update);

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
    private static readonly ArrayPool<byte> bytePool = ArrayPool<byte>.Shared;

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

    protected PEReader CreatePEReader(string assemblyName, Stream assemblyDataStream)
    {
        var decompressedStream = TryDecompressLZ4(assemblyName, assemblyDataStream);
        // Use the decompressed stream, or if null, i.e. it wasn't compressed, use the original.
        return new(decompressedStream ?? assemblyDataStream);
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
    private Stream? TryDecompressLZ4(string assemblyName, Stream inputStream)
    {
        const uint CompressedDataMagic = 0x5A4C4158; // 'XALZ', little-endian
        var reader = new BinaryReader(inputStream);
        if (reader.ReadUInt32() != CompressedDataMagic)
        {
            // Restore the input stream to the begininng if we're not decompressing.
            inputStream.Position = 0;
            return null;
        }
        reader.ReadUInt32(); // ignore descriptor index, we don't need it
        var decompressedLength = reader.ReadInt32();

        _logger?.LogDebug("Decompressing assembly ({0} bytes uncompressed) using LZ4", decompressedLength);

        // copy the compressed data
        Debug.Assert(inputStream.Position == 12);
        var inputLength = (int)(inputStream.Length - 12);
        var sourceBytes = bytePool.Rent(inputLength);
        try
        {
            reader.Read(sourceBytes, 0, inputLength);

            // We can release the reader & the input stream now.
            reader.Dispose();
            inputStream.Dispose();

            var outputStream = new MemoryStream(decompressedLength);

            // we're writing to the underlying array manually
            outputStream.SetLength(decompressedLength);
            var outputBuffer = outputStream.GetBuffer();

            var decoded = LZ4Codec.Decode(sourceBytes, 0, inputLength, outputBuffer, 0, decompressedLength);
            if (decoded != decompressedLength)
            {
                throw new Exception($"Failed to decompress LZ4 data of assembly {assemblyName} - decoded {decoded} instead of expected {decompressedLength} bytes");
            }
            return outputStream;
        }
        finally
        {
            bytePool.Return(sourceBytes);
        }
    }
}

// See https://devblogs.microsoft.com/dotnet/performance-improvements-in-dotnet-maui/#single-file-assembly-stores
internal sealed class AndroidAssemblyStoreReader : AndroidAssemblyReader, IAndroidAssemblyReader
{
    public AndroidAssemblyStoreReader(ZipArchive zip, IList<string> supportedAbis, IDiagnosticLogger? logger)
        : base(zip, supportedAbis, logger) { }

    public PEReader? TryReadAssembly(string name)
    {
        // TODO currently not supported
        return null;
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
        return CreatePEReader(name, zipEntry.Open());
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