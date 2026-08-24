namespace Sentry.Android.AssemblyReader.V2;

internal class AndroidAssemblyStoreReader : IAndroidAssemblyReader
{
    private readonly IList<AssemblyStoreExplorer> _explorers;
    private readonly DebugLogger? _logger;

    private AndroidAssemblyStoreReader(IList<AssemblyStoreExplorer> explorers, DebugLogger? logger)
    {
        _explorers = explorers;
        _logger = logger;
    }

    public static bool TryReadStore(string inputFile, IList<string> supportedAbis, DebugLogger? logger, [NotNullWhen(true)] out AndroidAssemblyStoreReader? reader)
    {
        List<AssemblyStoreExplorer> supportedExplorers = [];

        // First we check the base.apk for an assembly store
        var (explorers, errorMessage) = AssemblyStoreExplorer.Open(inputFile, logger);
        if (explorers is null)
        {
            logger?.Invoke(DebugLoggerLevel.Debug, "Unable to read store information for {0}: {1}", inputFile, errorMessage);

            // Check for assembly stores in any device specific APKs
            foreach (var supportedAbi in supportedAbis)
            {
                var splitFilePath = inputFile.GetArchivePathForAbi(supportedAbi, logger);
                if (!File.Exists(splitFilePath))
                {
                    logger?.Invoke(DebugLoggerLevel.Debug, "No split config detected at: '{0}'", splitFilePath);
                    continue;
                }
                (explorers, errorMessage) = AssemblyStoreExplorer.Open(splitFilePath, logger);
                if (explorers is not null)
                {
                    supportedExplorers.AddRange(explorers); // If the error is null then this is not null
                }
                else
                {
                    logger?.Invoke(DebugLoggerLevel.Debug, "Unable to read store information for {0}: {1}", splitFilePath, errorMessage);
                }
            }
        }
        else
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
            logger?.Invoke(DebugLoggerLevel.Debug, "Could not find V2 AssemblyStoreExplorer for the supported ABIs: {0}", string.Join(", ", supportedAbis));
            reader = null;
            return false;
        }

        reader = new AndroidAssemblyStoreReader(supportedExplorers, logger);
        return true;
    }

    public PEReader? TryReadAssembly(string name)
    {
        var explorerAssembly = TryFindAssembly(name);
        if (explorerAssembly is null)
        {
            _logger?.Invoke(DebugLoggerLevel.Debug, "Couldn't find assembly {0} in the APK AssemblyStore", name);
            return null;
        }

        var (explorer, storeItem) = explorerAssembly;
        _logger?.Invoke(DebugLoggerLevel.Debug, "Resolved assembly {0} in the APK {1} AssemblyStore", name, storeItem.TargetArch);

        var stream = explorer.ReadImageData(storeItem, false);
        if (stream is null)
        {
            _logger?.Invoke(DebugLoggerLevel.Debug, "Couldn't access assembly {0} image stream", name);
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

        // If the assembly name ends with .dll or .exe, try to find it without the extension.
        if ((IsFileType(".dll") || IsFileType(".exe")) && FindBestAssembly(name[..^4], out assembly))
        {
            return assembly;
        }

        // Conversely, if there is no extension, try with the dll extension (sometimes required for class libraries).
        // See: https://github.com/getsentry/sentry-dotnet/issues/4278#issuecomment-2986009125
        if (!IsFileType(".dll") && !IsFileType(".exe") && FindBestAssembly(name + ".dll", out assembly))
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
                _logger?.Invoke(DebugLoggerLevel.Debug, "Found best assembly {0} in APK AssemblyStore for target arch {1}", name, explorer.TargetArch);
                explorerAssembly = new(explorer, assembly);
                return true;
            }
        }
        _logger?.Invoke(DebugLoggerLevel.Warning, "No best assembly for {0} in APK AssemblyStore", name);
        explorerAssembly = null;
        return false;
    }

    private record ExplorerStoreItem(AssemblyStoreExplorer Explorer, AssemblyStoreItem StoreItem);

    public void Dispose()
    {
        // No-op
    }
}
