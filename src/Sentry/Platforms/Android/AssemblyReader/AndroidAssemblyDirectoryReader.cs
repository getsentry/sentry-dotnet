using System.Reflection.PortableExecutable;
using Sentry.Extensibility;

namespace Sentry.Android.AssemblyReader;

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
            Logger?.LogDebug("Couldn't find assembly {0} in the APK", name);
            return null;
        }

        Logger?.LogDebug("Resolved assembly {0} in the APK at {1}", name, zipEntry.FullName);

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
        var zipEntry = ZipArchive.GetEntry($"assemblies/{name}");

        if (zipEntry is null)
        {
            foreach (var abi in SupportedAbis)
            {
                if (abi.Length > 0)
                {
                    zipEntry = ZipArchive.GetEntry($"assemblies/{abi}/{name}");
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
