using System.IO.Compression;
using System.Reflection.PortableExecutable;
using Sentry.Extensibility;

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
        var zipArchive = ZipFile.OpenRead(apkPath);

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
    protected IDiagnosticLogger? _logger;
    protected ZipArchive _zipArchive;
    protected IList<string> _supportedAbis;

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

        using var zipStream = zipEntry.Open();

        // The DLL may be LZ4 compressed in addition to the APK being zipped.
        // See https://github.com/xamarin/xamarin-android/pull/4686
        // The format is:
        //    [ 4 byte magic header ] (XALZ)
        //    [ 4 byte header index ]
        //    [ 4 byte uncompressed payload length ]
        //    [rest: lz4 compressed payload]

        // Unfortunately, we can't just return `new PEReader(zipEntry.Open())` because it requires a seekable stream.
        // Therefore, we are making an in-memory copy of the DLL.
        var memStream = new MemoryStream((int)zipEntry.Length);
        zipStream.CopyTo(memStream);
        memStream.Position = 0;
        return new(memStream);
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