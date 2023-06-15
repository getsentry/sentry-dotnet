using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry.Internal.ILSpy;

#if NETCOREAPP3_0_OR_GREATER && PLATFORM_NEUTRAL

internal sealed class SingleFileApp
{
    private SingleFileApp(SingleFileBundle.Header manifest, List<BundleEntry> entries)
    {
        BundleHeader = manifest;
        Entries = entries;
    }

    private static readonly Lazy<SingleFileApp?> LazyMainModule = new(FromMainModule, LazyThreadSafetyMode.ExecutionAndPublication);
    public static SingleFileApp? MainModule => LazyMainModule.Value;

    public SingleFileBundle.Header BundleHeader { get; }

    public List<BundleEntry> Entries { get; }

    public DebugImage? GetDebugImage(Module module)
    {
        var entry = Entries.Find(e =>
            string.Equals(module.ScopeName, e.Name, StringComparison.OrdinalIgnoreCase)
            );

        return entry?.DebugImageData?.ToDebugImage(module.ScopeName, module.ModuleVersionId);
    }

    /// <summary>
    /// Mainly for testing purposes... this allows us get info about a single file that resides on disk.
    /// The more common scenario is to get info about the current process (which is where errors will be
    /// surfaced and caught by Sentry).
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    internal static SingleFileApp? FromFile(string fileName)
    {
        try
        {
            // Load the file into memory, check if it's a SingleFileBundle, extract the manifest and entries if so
            using var memoryMappedFile = MemoryMappedFile.CreateFromFile(fileName, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
            var view = memoryMappedFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
            try
            {
                if (!SingleFileBundle.IsBundle(view, out var bundleHeaderOffset))
                {
                    return null;
                }

                var manifest = SingleFileBundle.ReadManifest(view, bundleHeaderOffset);
                var entries = new List<BundleEntry>();
                foreach (var entry in manifest.Entries)
                {
                    using var stream = SingleFileBundle.TryOpenEntryStream(entry, view);
                    if (stream == null)
                    {
                        continue;
                    }

                    using var peReader = new PEReader(stream, PEStreamOptions.PrefetchEntireImage);
                    if (peReader.TryGetPEDebugImageData() is { } debugImageData)
                    {
                        entries.Add(new BundleEntry(fileName, entry, debugImageData));
                    }
                }
                var result = new SingleFileApp(manifest, entries);
                return result;
            }
            finally
            {
                view?.Dispose();
            }
        }
        catch (Exception ex)
        {
            SentrySdk.CurrentOptions?.LogDebug("Error loading Module from bundle {0}: {1}", fileName,  ex.Message);
            return null;
        }
    }

    internal static SingleFileApp? FromMainModule()
    {
        // Get the current process
        var currentProcess = Process.GetCurrentProcess();

        // Access the main module of the process
        var mainModule = currentProcess.MainModule;

        // Retrieve the path and filename of the main module
        if (mainModule?.FileName is not { } fileName)
        {
            Debug.WriteLine("Could not get main module file name.");
            return null;
        }

        return FromFile(fileName);
    }

    internal sealed class BundleEntry
    {
        internal readonly SingleFileBundle.Entry _entry;

        public BundleEntry(string bundleFile, SingleFileBundle.Entry entry, PEDebugImageData debugImageData)
        {
            BundleFile = bundleFile;
            DebugImageData = debugImageData;
            _entry = entry;
        }

        public string BundleFile { get; }
        public PEDebugImageData? DebugImageData { get; }
        public string Name => _entry.RelativePath;
        public string FullName => $"bundle://{BundleFile};{Name}";
    }
}

internal static class SingleFileAppExtensions
{
    internal static bool IsBundle(this SingleFileApp? singleFileApp) => singleFileApp is not null;
}

#endif

internal static class ModuleExtensions
{
    /// <summary>
    /// The Module.Name for Modules that are embedded in SingleFileApps will be null
    /// or &lt;Unknown&gt;, in that case we can use Module.ScopeName instead
    /// </summary>
    /// <param name="module">A Module instance</param>
    /// <returns>module.Name, if this is available. module.ScopeName otherwise</returns>
    public static string? GetNameOrScopeName(this Module module) =>
        (module?.Name is null || module.Name.Equals("<Unknown>"))
            ? module?.ScopeName
            : module?.Name;
}
