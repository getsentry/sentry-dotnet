using Sentry.Android.AssemblyReader.V2;

namespace Sentry.Android.AssemblyReader.V2;

// The "Old" app type - where each DLL is placed in the 'assemblies' directory as an individual file.
internal sealed class AndroidAssemblyDirectoryReaderV2 : IAndroidAssemblyReader
{
    private DebugLogger? Logger { get; }
    private HashSet<AndroidTargetArch> SupportedArchitectures { get; } = new();
    private readonly ArchiveAssemblyHelper _archiveAssemblyHelper;

    public AndroidAssemblyDirectoryReaderV2(string apkPath, IList<string> supportedAbis, DebugLogger? logger)
    {
        Logger = logger;
        foreach (var abi in supportedAbis)
        {
            SupportedArchitectures.Add(abi.AbiToDeviceArchitecture());
        }
        _archiveAssemblyHelper = new ArchiveAssemblyHelper(apkPath, logger, false);
    }

    public PEReader? TryReadAssembly(string name)
    {
        if (File.Exists(name))
        {
            // The assembly is already extracted to the file system.  Just read it.
            var stream = File.OpenRead(name);
            return new PEReader(stream);
        }

        foreach (var arch in SupportedArchitectures)
        {
            if (_archiveAssemblyHelper.ReadEntry($"assemblies/{name}", arch) is not { } memStream)
            {
                continue;
            }

            Logger?.Invoke("Resolved assembly {0} in the APK", name);
            return AndroidAssemblyReader.CreatePEReader(name, memStream, Logger);
        }

        Logger?.Invoke("Couldn't find assembly {0} in the APK", name);
        return null;
    }

    public void Dispose()
    {
        // No-op
    }
}
