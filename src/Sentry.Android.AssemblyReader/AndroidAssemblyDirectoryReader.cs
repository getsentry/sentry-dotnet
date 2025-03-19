using Sentry.Android.AssemblyReader.V1;
using Sentry.Android.AssemblyReader.V2;

namespace Sentry.Android.AssemblyReader;

// The "Old" app type - where each DLL is placed in the 'assemblies' directory as an individual file.
internal sealed class AndroidAssemblyDirectoryReader : AndroidAssemblyReader, IAndroidAssemblyReader
{
    public AndroidAssemblyDirectoryReader(ZipArchive zip, IList<string> supportedAbis, DebugLogger? logger)
        : base(zip, supportedAbis, logger) { }

    public PEReader? TryReadAssembly(string name)
    {
        if (File.Exists(name))
        {
            // The assembly is already extracted to the file system.  Just read it.
            var stream = File.OpenRead(name);
            return new PEReader(stream);
        }

        var zipEntry = FindAssembly(name);
        if (zipEntry is null)
        {
            Logger?.Invoke("Couldn't find assembly {0} in the APK", name);
            return null;
        }

        Logger?.Invoke("Resolved assembly {0} in the APK at {1}", name, zipEntry.FullName);

        // We need a seekable stream for the PEReader (or even to check whether the DLL is compressed), so make a copy.
        var memStream = zipEntry.Extract();
        return CreatePEReader(name, memStream, Logger);
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
