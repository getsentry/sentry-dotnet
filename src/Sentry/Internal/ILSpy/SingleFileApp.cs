using System.IO.MemoryMappedFiles;
using System.Reflection;
using System.Reflection.Metadata;
using System.Xml.Linq;
using Sentry.Extensibility;
using Sentry.Internal.ILSpy;

namespace Sentry.Internal.ILSply;

internal class SingleFileApp
{
    public SingleFileBundle.Header BundleHeader => _bundleHeader;
    private readonly SingleFileBundle.Header _bundleHeader;

    public List<BundleEntry> Entries => _entries;
    private readonly List<BundleEntry> _entries;


    public SingleFileApp(SingleFileBundle.Header manifest, List<BundleEntry> entries)
    {
        _bundleHeader = manifest;
        _entries = entries;
    }

    internal static PEReader? GetEmbeddedAssembly(Module module, out string? assemblyName)
    {
        if (SingleFileApp.FromMainModule() is { } singleFileApp)
        {
            // TODO: Track down the appropriate entry, use the offset to work out where
            // it is in memory and create a PEReader for it
            var entry = singleFileApp?.Entries.FirstOrDefault(e => string.Equals(module.ScopeName, e.Name, StringComparison.OrdinalIgnoreCase));
            if (entry != null)
            {
                Stream? stream = entry.TryOpenStream();

                if (stream != null)
                {
                    // Read the module from a precrafted stream
                    if (!stream.CanSeek)
                    {
                        var memoryStream = new MemoryStream();
                        stream.CopyTo(memoryStream);
                        stream.Close();
                        memoryStream.Position = 0;
                        stream = memoryStream;
                    }
                    assemblyName = module.ScopeName; // TODO: This won't work... need to find a better "key" to track down the entry
                    return new PEReader(stream, PEStreamOptions.PrefetchEntireImage);
                }
            }
        }
        assemblyName = null;
        return null;
    }

    internal static SingleFileApp? FromMainModule()
    {
        // Get the file name of the current process
        if (GetMainModuleFileName() is not { } fileName)
        {
            Debug.WriteLine("Could not get main module file name.");
            return null;
        }

        // Load the file into memory, check if it's a SingleFileBundle, extract the manifest and entries if so
        using var memoryMappedFile = MemoryMappedFile.CreateFromFile(fileName, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
        var view = memoryMappedFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
        try
        {
            if (!SingleFileBundle.IsBundle(view, out long bundleHeaderOffset))
                return null;
            var manifest = SingleFileBundle.ReadManifest(view, bundleHeaderOffset);
            var entries = manifest.Entries.Select(e => new BundleEntry(fileName, view, e)).ToList();
            var result = new SingleFileApp(manifest, entries);
            view = null; // don't dispose the view, we're still using it in the bundle entries
            return result;
        }
        finally
        {
            view?.Dispose();
        }
    }

    internal static string? GetMainModuleFileName()
    {
        // Get the current process
        Process currentProcess = Process.GetCurrentProcess();

        // Access the main module of the process
        var mainModule = currentProcess.MainModule;

        // Retrieve the path and filename of the main module
        return mainModule?.FileName;
    }

    internal sealed class BundleEntry : PackageEntry
    {
        internal readonly string bundleFile;
        internal readonly MemoryMappedViewAccessor view;
        internal readonly SingleFileBundle.Entry entry;

        public BundleEntry(string bundleFile, MemoryMappedViewAccessor view, SingleFileBundle.Entry entry)
        {
            this.bundleFile = bundleFile;
            this.view = view;
            this.entry = entry;
        }

        public override string Name => entry.RelativePath;
        public override string FullName => $"bundle://{bundleFile};{Name}";

        public override Stream TryOpenStream()
        {
            Debug.WriteLine("Open bundle member " + Name);

            if (entry.CompressedSize == 0)
            {
                return new UnmanagedMemoryStream(view.SafeMemoryMappedViewHandle, entry.Offset, entry.Size);
            }
            else
            {
                Stream compressedStream = new UnmanagedMemoryStream(view.SafeMemoryMappedViewHandle, entry.Offset, entry.CompressedSize);
                using var deflateStream = new DeflateStream(compressedStream, CompressionMode.Decompress);
                Stream decompressedStream = new MemoryStream((int)entry.Size);
                deflateStream.CopyTo(decompressedStream);
                if (decompressedStream.Length != entry.Size)
                {
                    throw new InvalidDataException($"Corrupted single-file entry '{entry.RelativePath}'. Declared decompressed size '{entry.Size}' is not the same as actual decompressed size '{decompressedStream.Length}'.");
                }

                decompressedStream.Seek(0, SeekOrigin.Begin);
                return decompressedStream;
            }
        }

        public override long? TryGetLength()
        {
            return entry.Size;
        }
    }

    internal abstract class PackageEntry : Resource
    {
        /// <summary>
        /// Gets the file name of the entry (may include path components, relative to the package root).
        /// </summary>
        public abstract override string Name { get; }

        /// <summary>
        /// Gets the full file name for the entry.
        /// </summary>
        public abstract string FullName { get; }
    }
}

internal static class ModuleExtensions
{
    /// <summary>
    /// The Module.Name for Modules that are embedded in SingleFileApps will be null
    /// or &lt;Unknown&gt;, in that case we can use Module.ScopeName instead
    /// </summary>
    /// <param name="module">A Module instance</param>
    /// <returns>module.Name, if this is available. module.ScopeName otherwise</returns>
    public static string? ModuleNameOrScopeName(this Module module) =>
        (module?.Name is null || module.Name.Equals("<Unknown>"))
            ? module?.ScopeName
            : module?.Name;
}
