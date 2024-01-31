using Sentry.Extensibility;
using Sentry.Internal.Extensions;
using Sentry.Protocol;

namespace Sentry.Internal.ILSpy;

#if NET5_0_OR_GREATER && PLATFORM_NEUTRAL

internal sealed class SingleFileApp
{
    private SingleFileApp(SingleFileBundle.Header manifest, BundleEntries entries)
    {
        BundleHeader = manifest;
        _entries = new ConcurrentDictionary<string, PEDebugImageData>(entries.ToDictionary(
            e => e.Key,
            e => e.Value
        ), StringComparer.OrdinalIgnoreCase);
    }

    private static readonly Lazy<SingleFileApp?> LazyMainModule = new(FromMainModule, LazyThreadSafetyMode.ExecutionAndPublication);
    public static SingleFileApp? MainModule => LazyMainModule.Value;

    public SingleFileBundle.Header BundleHeader { get; }

    private readonly ConcurrentDictionary<string, PEDebugImageData> _entries;

    public DebugImage? GetDebugImage(Module module)
    {
        return (_entries.TryGetValue(module.ScopeName, out var debugImageData))
            ? debugImageData?.ToDebugImage(module.ScopeName, module.ModuleVersionId)
            : null;
    }

    /// <summary>
    /// Adapted from the LoadedPackage.FromBundle method in ILSpy:
    /// https://github.com/icsharpcode/ILSpy/blob/311658c7109c3872e020cba2525b1b3a371d5813/ICSharpCode.ILSpyX/LoadedPackage.cs#L111
    /// commit a929fcb5202824e3c061f4824c7fc9ba867d55af
    ///
    /// Load a .NET single-file bundle.
    /// </summary>
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
                var entries = new BundleEntries();
                foreach (var entry in manifest.Entries)
                {
                    using var stream = TryOpenEntryStream(entry, view);
                    using var peReader = new PEReader(stream, PEStreamOptions.PrefetchEntireImage);
                    if (peReader.TryGetPEDebugImageData() is { } debugImageData)
                    {
                        entries.Add(entry.RelativePath, debugImageData);
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
            SentrySdk.CurrentOptions?.LogDebug("Error loading Module from bundle {0}: {1}", fileName, ex.Message);
            return null;
        }
    }

    private static SingleFileApp? FromMainModule()
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

    /// <summary>
    /// Adapted from BundleEntryDebugData.TryOpenStream in ILSpy:
    /// https://github.com/icsharpcode/ILSpy/blob/311658c7109c3872e020cba2525b1b3a371d5813/ICSharpCode.ILSpyX/LoadedPackage.cs#L208
    /// commit a929fcb5202824e3c061f4824c7fc9ba867d55af
    /// </summary>
    /// <exception cref="InvalidDataException"></exception>
    private static Stream TryOpenEntryStream(SingleFileBundle.Entry entry, MemoryMappedViewAccessor view)
    {
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

    private sealed class BundleEntries : List<KeyValuePair<string, PEDebugImageData>>
    {
    }
}

internal static class SingleFileAppExtensions
{
    internal static bool IsBundle(this SingleFileApp? singleFileApp) => singleFileApp is not null;
}

#endif
