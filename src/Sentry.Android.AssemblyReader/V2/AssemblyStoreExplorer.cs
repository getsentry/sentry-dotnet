/*
 * Adapted from https://github.com/dotnet/android/blob/5ebcb1dd1503648391e3c0548200495f634d90c6/tools/assembly-store-reader-mk2/AssemblyStore/AssemblyStoreExplorer.cs
 * Original code licensed under the MIT License (https://github.com/dotnet/android/blob/5ebcb1dd1503648391e3c0548200495f634d90c6/LICENSE.TXT)
 */

namespace Sentry.Android.AssemblyReader.V2;

internal class AssemblyStoreExplorer
{
    private readonly AssemblyStoreReader _reader;

    public AndroidTargetArch? TargetArch { get; }
    public IList<AssemblyStoreItem>? Assemblies { get; }
    public IDictionary<string, AssemblyStoreItem>? AssembliesByName { get; }
    public bool Is64Bit { get; }

    private AssemblyStoreExplorer(Stream storeStream, string path, DebugLogger? logger)
    {
        var storeReader = AssemblyStoreReader.Create(storeStream, path, logger);
        if (storeReader == null)
        {
            storeStream.Dispose();
            throw new NotSupportedException($"Format of assembly store '{path}' is unsupported");
        }

        _reader = storeReader;
        TargetArch = _reader.TargetArch;
        Assemblies = _reader.Assemblies;
        Is64Bit = _reader.Is64Bit;

        var dict = new Dictionary<string, AssemblyStoreItem>(StringComparer.Ordinal);
        if (Assemblies is not null)
        {
            foreach (var item in Assemblies)
            {
                dict.Add(item.Name, item);
            }
        }
        AssembliesByName = dict.AsReadOnly();
    }

    private AssemblyStoreExplorer(FileInfo storeInfo, DebugLogger? logger)
        : this(storeInfo.OpenRead(), storeInfo.FullName, logger)
    { }

    public static (IList<AssemblyStoreExplorer>? explorers, string? errorMessage) Open(string inputFile, DebugLogger? logger)
    {
        var (format, info) = Utils.DetectFileFormat(inputFile);
        if (info == null)
        {
            return (null, $"File '{inputFile}' does not exist.");
        }

        switch (format)
        {
            case FileFormat.Unknown:
                return (null, $"File '{inputFile}' has an unknown format.");

            case FileFormat.Zip:
                return (null, $"File '{inputFile}' is a ZIP archive, but not an Android one.");

            case FileFormat.AssemblyStore:
            case FileFormat.ELF:
                return (new List<AssemblyStoreExplorer> { new AssemblyStoreExplorer(info, logger) }, null);

            case FileFormat.Aab:
                return OpenAab(info, logger);

            case FileFormat.AabBase:
                return OpenAabBase(info, logger);

            case FileFormat.Apk:
                return OpenApk(info, logger);

            default:
                return (null, $"File '{inputFile}' has an unsupported format '{format}'");
        }
    }

    private static (IList<AssemblyStoreExplorer>? explorers, string? errorMessage) OpenAab(FileInfo fi, DebugLogger? logger)
        => OpenCommon(fi, [StoreReaderV2.AabPaths, StoreReader_V1.AabPaths], logger);

    private static (IList<AssemblyStoreExplorer>? explorers, string? errorMessage) OpenAabBase(FileInfo fi, DebugLogger? logger)
        => OpenCommon(fi, [StoreReaderV2.AabBasePaths, StoreReader_V1.AabBasePaths], logger);

    private static (IList<AssemblyStoreExplorer>? explorers, string? errorMessage) OpenApk(FileInfo fi, DebugLogger? logger)
        => OpenCommon(fi, [StoreReaderV2.ApkPaths, StoreReader_V1.ApkPaths], logger);

    private static (IList<AssemblyStoreExplorer>? explorers, string? errorMessage) OpenCommon(FileInfo fi, List<IList<string>> pathLists, DebugLogger? logger)
    {
        using var zip = ZipFile.OpenRead(fi.FullName);

        foreach (var paths in pathLists)
        {
            var (explorers, errorMessage, pathsFound) = TryLoad(fi, zip, paths, logger);
            if (pathsFound)
            {
                return (explorers, errorMessage);
            }
        }

        return (null, "Unable to find any blob entries");
    }

    private static (IList<AssemblyStoreExplorer>? explorers, string? errorMessage, bool pathsFound) TryLoad(FileInfo fi, ZipArchive zip, IList<string> paths, DebugLogger? logger)
    {
        var ret = new List<AssemblyStoreExplorer>();

        foreach (var path in paths)
        {
            if (zip.GetEntry(path) is not { } entry)
            {
                continue;
            }

            var stream = entry.Extract();
            ret.Add(new AssemblyStoreExplorer(stream, $"{fi.FullName}!{path}", logger));
        }

        if (ret.Count == 0)
        {
            return (null, null, false);
        }

        return (ret, null, true);
    }

    public MemoryStream? ReadImageData(AssemblyStoreItem item, bool uncompressIfNeeded = false)
    {
        return _reader.ReadEntryImageData(item, uncompressIfNeeded);
    }
}
