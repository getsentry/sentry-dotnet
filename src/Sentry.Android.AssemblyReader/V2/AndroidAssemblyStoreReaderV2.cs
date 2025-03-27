namespace Sentry.Android.AssemblyReader.V2;

internal class AndroidAssemblyStoreReaderV2 : IAndroidAssemblyReader
{
    private readonly IList<AssemblyStoreExplorer> _explorers;
    private readonly DebugLogger? _logger;

    private AndroidAssemblyStoreReaderV2(IList<AssemblyStoreExplorer> explorers, DebugLogger? logger)
    {
        _explorers = explorers;
        _logger = logger;
    }

    public static bool TryReadStore(string inputFile, IList<string> supportedAbis, DebugLogger? logger, [NotNullWhen(true)] out AndroidAssemblyStoreReaderV2? reader)
    {
        var (explorers, errorMessage) = AssemblyStoreExplorer.Open(inputFile, logger);
        if (errorMessage != null)
        {
            logger?.Invoke(errorMessage);
            reader = null;
            return false;
        }

        List<AssemblyStoreExplorer> supportedExplorers = [];
        if (explorers is not null)
        {
            foreach (var explorer in explorers)
            {
                if (explorer.TargetArch is null)
                {
                    continue;
                }

                foreach (var supportedAbi in supportedAbis)
                {
                    if (supportedAbi.AbiToDeviceArchitecture() == explorer.TargetArch)
                    {
                        supportedExplorers.Add(explorer);
                    }
                }
            }
        }

        if (supportedExplorers.Count == 0)
        {
            logger?.Invoke("Could not find V2 AssemblyStoreExplorer for the supported ABIs: {0}", string.Join(", ", supportedAbis));
            reader = null;
            return false;
        }

        reader = new AndroidAssemblyStoreReaderV2(supportedExplorers, logger);
        return true;
    }

    public PEReader? TryReadAssembly(string name)
    {
        var explorerAssembly = TryFindAssembly(name);
        if (explorerAssembly is null)
        {
            _logger?.Invoke("Couldn't find assembly {0} in the APK AssemblyStore", name);
            return null;
        }

        var (explorer, storeItem) = explorerAssembly;
        _logger?.Invoke("Resolved assembly {0} in the APK {1} AssemblyStore", name, storeItem.TargetArch);

        var stream = explorer.ReadImageData(storeItem, false);
        if (stream is null)
        {
            _logger?.Invoke("Couldn't access assembly {0} image stream", name);
            return null;
        }

        return ArchiveUtils.CreatePEReader(name, stream, _logger);
    }

    private ExplorerStoreItem? TryFindAssembly(string name)
    {
        if (FindBestAssembly(name, out var assembly))
        {
            return assembly;
        }

        if ((IsFileType(".dll") || IsFileType(".exe")) && FindBestAssembly(name[..^4], out assembly))
        {
            return assembly;
        }

        return null;

        bool IsFileType(string extension)
        {
            return name.EndsWith(extension, ignoreCase: true, CultureInfo.InvariantCulture);
        }
    }

    private bool FindBestAssembly(string name, out ExplorerStoreItem? explorerAssembly)
    {
        foreach (var explorer in _explorers)
        {
            if (explorer.AssembliesByName?.TryGetValue(name, out var assembly) is true)
            {
                explorerAssembly = new(explorer, assembly);
                return true;
            }
        }
        explorerAssembly = null;
        return false;
    }

    private record ExplorerStoreItem(AssemblyStoreExplorer Explorer, AssemblyStoreItem StoreItem);

    public void Dispose()
    {
        // No-op
    }
}
