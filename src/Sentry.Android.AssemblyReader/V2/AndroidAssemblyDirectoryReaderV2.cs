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
        _archiveAssemblyHelper = new ArchiveAssemblyHelper(apkPath, logger);
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
            return ArchiveUtils.CreatePEReader(name, memStream, Logger);
        }

        Logger?.Invoke("Couldn't find assembly {0} in the APK", name);
        return null;
    }

    public void Dispose()
    {
        // No-op
    }

    /*
     * Adapted from https://github.com/dotnet/android/blob/6394773fad5108b0d7b4e6f087dc3e6ea997401a/src/Xamarin.Android.Build.Tasks/Tests/Xamarin.Android.Build.Tests/Utilities/ArchiveAssemblyHelper.cs
     * Original code licensed under the MIT License (https://github.com/dotnet/android-tools/blob/ab2165daf27d4fcb29e88bc022e0ab0be33aff69/LICENSE)
     */
    internal class ArchiveAssemblyHelper
    {
        private static readonly ArrayPool<byte> Buffers = ArrayPool<byte>.Shared;

        private readonly string _archivePath;
        private readonly DebugLogger? _logger;

        public ArchiveAssemblyHelper(string archivePath, DebugLogger? logger)
        {
            if (string.IsNullOrEmpty(archivePath))
            {
                throw new ArgumentException("must not be null or empty", nameof(archivePath));
            }

            _archivePath = archivePath;
            _logger = logger;
        }

        public MemoryStream? ReadEntry(string path, AndroidTargetArch arch = AndroidTargetArch.None, bool uncompressIfNecessary = false)
        {
            var ret = ReadZipEntry(path, arch);
            if (ret == null)
            {
                return null;
            }

            ret.Flush();
            ret.Seek(0, SeekOrigin.Begin);
            var (elfPayloadOffset, elfPayloadSize, error) = Utils.FindELFPayloadSectionOffsetAndSize(ret);

            if (error != ELFPayloadError.None)
            {
                var message = error switch
                {
                    ELFPayloadError.NotELF => $"Entry '{path}' is not a valid ELF binary",
                    ELFPayloadError.LoadFailed => $"Entry '{path}' could not be loaded",
                    ELFPayloadError.NotSharedLibrary => $"Entry '{path}' is not a shared ELF library",
                    ELFPayloadError.NotLittleEndian => $"Entry '{path}' is not a little-endian ELF image",
                    ELFPayloadError.NoPayloadSection => $"Entry '{path}' does not contain the 'payload' section",
                    _ => $"Unknown ELF payload section error for entry '{path}': {error}"
                };
                _logger?.Invoke(message);
            }
            else
            {
                _logger?.Invoke($"Extracted content from ELF image '{path}'");
            }

            if (elfPayloadOffset == 0)
            {
                ret.Seek(0, SeekOrigin.Begin);
                return ret;
            }

            // Make a copy of JUST the payload section, so that it contains only the data the tests expect and support
            var payload = new MemoryStream();
            var data = Buffers.Rent(16384);
            var toRead = data.Length;
            var nRead = 0;
            var remaining = elfPayloadSize;

            ret.Seek((long)elfPayloadOffset, SeekOrigin.Begin);
            while (remaining > 0 && (nRead = ret.Read(data, 0, toRead)) > 0)
            {
                payload.Write(data, 0, nRead);
                remaining -= (ulong)nRead;

                if (remaining < (ulong)data.Length)
                {
                    // Make sure the last chunk doesn't gobble in more than we need
                    toRead = (int)remaining;
                }
            }
            Buffers.Return(data);

            payload.Flush();
            ret.Dispose();

            payload.Seek(0, SeekOrigin.Begin);
            return payload;
        }

        private MemoryStream? ReadZipEntry(string path, AndroidTargetArch arch)
        {
            var potentialEntries = TransformArchiveAssemblyPath(path, arch);
            if (potentialEntries == null || potentialEntries.Count == 0)
            {
                return null;
            }

            using var zip = ZipFile.OpenRead(_archivePath);
            foreach (var assemblyPath in potentialEntries)
            {
                if (zip.GetEntry(assemblyPath) is not { } entry)
                {
                    continue;
                }

                var ret = entry.Extract();
                ret.Flush();
                return ret;
            }

            return null;
        }

        /// <summary>
        /// Takes "old style" `assemblies/assembly.dll` path and returns (if possible) a set of paths that reflect the new
        /// location of `lib/{ARCH}/assembly.dll.so`. A list is returned because, if `arch` is `None`, we'll return all
        /// the possible architectural paths.
        /// An exception is thrown if we cannot transform the path for some reason. It should **not** be handled.
        /// </summary>
        private static List<string>? TransformArchiveAssemblyPath(string path, AndroidTargetArch arch)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException(nameof(path), "must not be null or empty");
            }

            if (!path.StartsWith("assemblies/", StringComparison.Ordinal))
            {
                return [path];
            }

            var parts = path.Split('/');
            if (parts.Length < 2)
            {
                throw new InvalidOperationException($"Path '{path}' must consist of at least two segments separated by `/`");
            }

            // We accept:
            //   assemblies/assembly.dll
            //   assemblies/{CULTURE}/assembly.dll
            //   assemblies/{ABI}/assembly.dll
            //   assemblies/{ABI}/{CULTURE}/assembly.dll
            if (parts.Length > 4)
            {
                throw new InvalidOperationException($"Path '{path}' must not consist of more than 4 segments separated by `/`");
            }

            string? fileName = null;
            string? culture = null;
            string? abi = null;

            switch (parts.Length)
            {
                // Full satellite assembly path, with abi
                case 4:
                    abi = parts[1];
                    culture = parts[2];
                    fileName = parts[3];
                    break;

                // Assembly path with abi or culture
                case 3:
                    // If the middle part isn't a valid abi, we treat it as a culture name
                    if (MonoAndroidHelper.IsValidAbi(parts[1]))
                    {
                        abi = parts[1];
                    }
                    else
                    {
                        culture = parts[1];
                    }
                    fileName = parts[2];
                    break;

                // Assembly path without abi or culture
                case 2:
                    fileName = parts[1];
                    break;
            }

            var fileTypeMarker = MonoAndroidHelper.MANGLED_ASSEMBLY_REGULAR_ASSEMBLY_MARKER;
            var abis = new List<string>();
            if (!string.IsNullOrEmpty(abi))
            {
                abis.Add(abi);
            }
            else if (arch == AndroidTargetArch.None)
            {
                foreach (AndroidTargetArch targetArch in MonoAndroidHelper.SupportedTargetArchitectures)
                {
                    abis.Add(MonoAndroidHelper.ArchToAbi(targetArch));
                }
            }
            else
            {
                abis.Add(MonoAndroidHelper.ArchToAbi(arch));
            }

            if (!string.IsNullOrEmpty(culture))
            {
                // Android doesn't allow us to put satellite assemblies in lib/{CULTURE}/assembly.dll.so, we must instead
                // mangle the name.
                fileTypeMarker = MonoAndroidHelper.MANGLED_ASSEMBLY_SATELLITE_ASSEMBLY_MARKER;
                fileName = $"{culture}{MonoAndroidHelper.SATELLITE_CULTURE_END_MARKER_CHAR}{fileName}";
            }

            var ret = new List<string>();
            var newParts = new List<string> {
                string.Empty, // ABI placeholder
			    $"{fileTypeMarker}{fileName}.so",
            };

            foreach (var a in abis)
            {
                newParts[0] = a;
                ret.Add(MonoAndroidHelper.MakeZipArchivePath("lib", newParts));
            }

            return ret;
        }
    }
}
