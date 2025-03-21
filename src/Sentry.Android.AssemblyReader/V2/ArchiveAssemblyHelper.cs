/*
 * Adapted from https://github.com/dotnet/android/blob/6394773fad5108b0d7b4e6f087dc3e6ea997401a/src/Xamarin.Android.Build.Tasks/Tests/Xamarin.Android.Build.Tests/Utilities/ArchiveAssemblyHelper.cs
 * Original code licensed under the MIT License (https://github.com/dotnet/android-tools/blob/ab2165daf27d4fcb29e88bc022e0ab0be33aff69/LICENSE)
 */
namespace Sentry.Android.AssemblyReader.V2;

internal class ArchiveAssemblyHelper
{
    private const string DefaultAssemblyStoreEntryPrefix = "{storeReader}";

    private static readonly HashSet<string> SpecialExtensions = new (StringComparer.OrdinalIgnoreCase) {
		".dll",
		".config",
		".pdb",
	};

    private static readonly ArrayPool<byte> Buffers = ArrayPool<byte>.Shared;

    private readonly string _archivePath;
    private readonly DebugLogger? _logger;
    private readonly string _assembliesRootDir;
    private readonly bool _useAssemblyStores;
    private List<string>? _archiveContents;

	public ArchiveAssemblyHelper (string archivePath, DebugLogger? logger, bool useAssemblyStores = true)
	{
		if (string.IsNullOrEmpty (archivePath)) {
			throw new ArgumentException ("must not be null or empty", nameof (archivePath));
		}

		_archivePath = archivePath;
        _logger = logger;
        _useAssemblyStores = useAssemblyStores;

		var extension = Path.GetExtension (archivePath) ?? string.Empty;
		if (string.Compare (".aab", extension, StringComparison.OrdinalIgnoreCase) == 0) {
			_assembliesRootDir = "base/lib/";
		} else if (string.Compare (".apk", extension, StringComparison.OrdinalIgnoreCase) == 0) {
			_assembliesRootDir = "lib/";
		} else if (string.Compare (".zip", extension, StringComparison.OrdinalIgnoreCase) == 0) {
			_assembliesRootDir = "lib/";
		} else {
			_assembliesRootDir = string.Empty;
		}
	}

	public MemoryStream? ReadEntry (string path, AndroidTargetArch arch = AndroidTargetArch.None, bool uncompressIfNecessary = false)
	{
		var ret = _useAssemblyStores
            ? ReadStoreEntry (path, arch, uncompressIfNecessary)
            : ReadZipEntry (path, arch);

		if (ret == null) {
			return null;
		}

		ret.Flush ();
		ret.Seek (0, SeekOrigin.Begin);
		var (elfPayloadOffset, elfPayloadSize, error) = Utils.FindELFPayloadSectionOffsetAndSize (ret);

		if (error != ELFPayloadError.None) {
			var message = error switch {
				ELFPayloadError.NotELF           => $"Entry '{path}' is not a valid ELF binary",
				ELFPayloadError.LoadFailed       => $"Entry '{path}' could not be loaded",
				ELFPayloadError.NotSharedLibrary => $"Entry '{path}' is not a shared ELF library",
				ELFPayloadError.NotLittleEndian  => $"Entry '{path}' is not a little-endian ELF image",
				ELFPayloadError.NoPayloadSection => $"Entry '{path}' does not contain the 'payload' section",
				_                                => $"Unknown ELF payload section error for entry '{path}': {error}"
			};
			Console.WriteLine (message);
		} else {
			Console.WriteLine ($"Extracted content from ELF image '{path}'");
		}

		if (elfPayloadOffset == 0) {
			ret.Seek (0, SeekOrigin.Begin);
			return ret;
		}

		// Make a copy of JUST the payload section, so that it contains only the data the tests expect and support
		var payload = new MemoryStream ();
		var data = Buffers.Rent (16384);
		var toRead = data.Length;
		var nRead = 0;
		var remaining = elfPayloadSize;

		ret.Seek ((long)elfPayloadOffset, SeekOrigin.Begin);
		while (remaining > 0 && (nRead = ret.Read (data, 0, toRead)) > 0) {
			payload.Write (data, 0, nRead);
			remaining -= (ulong)nRead;

			if (remaining < (ulong)data.Length) {
				// Make sure the last chunk doesn't gobble in more than we need
				toRead = (int)remaining;
			}
		}
		Buffers.Return (data);

		payload.Flush ();
		ret.Dispose ();

		payload.Seek (0, SeekOrigin.Begin);
		return payload;
	}

    private MemoryStream? ReadZipEntry (string path, AndroidTargetArch arch)
	{
		var potentialEntries = TransformArchiveAssemblyPath (path, arch);
		if (potentialEntries == null || potentialEntries.Count == 0) {
			return null;
		}

		using var zip = ZipFile.OpenRead(_archivePath);
		foreach (var assemblyPath in potentialEntries) {
            if (zip.GetEntry(assemblyPath) is not {} entry)
            {
                continue;
			}

			var ret = entry.Extract();
			ret.Flush();
			return ret;
		}

		return null;
	}

    private MemoryStream? ReadStoreEntry (string path, AndroidTargetArch arch, bool uncompressIfNecessary)
	{
		var name = Path.GetFileNameWithoutExtension (path);
		var (explorers, errorMessage) = AssemblyStoreExplorer.Open(_archivePath, _logger);
		var explorer = SelectExplorer (explorers, arch);
		if (explorer == null) {
			Console.WriteLine ($"Failed to read assembly '{name}' from '{_archivePath}'. {errorMessage}");
			return null;
		}

		if (arch == AndroidTargetArch.None) {
			if (explorer.TargetArch == null) {
				throw new InvalidOperationException ($"Internal error: explorer should not have its TargetArch unset");
			}

			arch = (AndroidTargetArch)explorer.TargetArch;
		}

		Console.WriteLine ($"Trying to read store entry: {name}");
		var assemblies = explorer.Find (name, arch);
		if (assemblies == null) {
			Console.WriteLine ($"Failed to locate assembly '{name}' in assembly store for architecture '{arch}', in archive '{_archivePath}'");
			return null;
		}

		AssemblyStoreItem? assembly = null;
		foreach (var item in assemblies) {
			if (arch == AndroidTargetArch.None || item.TargetArch == arch) {
				assembly = item;
				break;
			}
		}

		if (assembly == null) {
			Console.WriteLine ($"Failed to find assembly '{name}' in assembly store for architecture '{arch}', in archive '{_archivePath}'");
			return null;
		}

		return explorer.ReadImageData (assembly, uncompressIfNecessary);
	}

	public List<string> ListArchiveContents (string storeEntryPrefix = DefaultAssemblyStoreEntryPrefix, bool forceRefresh = false, AndroidTargetArch arch = AndroidTargetArch.None)
	{
		if (!forceRefresh && _archiveContents != null) {
			return _archiveContents;
		}

		if (string.IsNullOrEmpty (storeEntryPrefix)) {
			throw new ArgumentException (nameof (storeEntryPrefix), "must not be null or empty");
		}

		var entries = new List<string> ();
		using (var zip = ZipFile.OpenRead(_archivePath)) {
			foreach (var entry in zip.Entries) {
				entries.Add(entry.FullName);
			}
		}

		_archiveContents = entries;
		if (!_useAssemblyStores) {
			Console.WriteLine ("Not using assembly stores");
			return entries;
		}

		Console.WriteLine ($"Creating AssemblyStoreExplorer for archive '{_archivePath}'");
		var (explorers, errorMessage) = AssemblyStoreExplorer.Open(_archivePath, _logger);

		if (arch == AndroidTargetArch.None) {
			if (explorers == null || explorers.Count == 0) {
				return entries;
			}

			foreach (var explorer in explorers) {
				SynthetizeAssemblies (explorer);
			}
		} else {
			SynthetizeAssemblies (SelectExplorer (explorers, arch));
		}

		Console.WriteLine ("Archive entries with synthetised assembly storeReader entries:");
		foreach (string e in entries) {
			Console.WriteLine ($"  {e}");
		}

		return entries;

		void SynthetizeAssemblies (AssemblyStoreExplorer? explorer)
		{
			if (explorer == null) {
				return;
			}

			Console.WriteLine ($"Explorer for {explorer.TargetArch} found {explorer.AssemblyCount} assemblies");
            if (explorer.Assemblies is null)
            {
                return;
            }

			foreach (var asm in explorer.Assemblies) {
				var prefix = storeEntryPrefix;
				var abi = MonoAndroidHelper.ArchToAbi (asm.TargetArch);
				prefix = $"{prefix}{abi}/";

				var cultureIndex = asm.Name.IndexOf ('/');
				string? culture = null;
				string name;

				if (cultureIndex > 0) {
					culture = asm.Name.Substring (0, cultureIndex);
					name = asm.Name.Substring (cultureIndex + 1);
				} else {
					name = asm.Name;
				}

				// Mangle name in the same fashion the discrete assembly entries are named, makes other
				// code in this class simpler.
				var mangledName = MonoAndroidHelper.MakeDiscreteAssembliesEntryName (name, culture);
				entries.Add ($"{prefix}{mangledName}");
				if (asm.DebugOffset > 0) {
					mangledName = MonoAndroidHelper.MakeDiscreteAssembliesEntryName (Path.ChangeExtension (name, "pdb"));
					entries.Add ($"{prefix}{mangledName}");
				}

				if (asm.ConfigOffset > 0) {
					mangledName = MonoAndroidHelper.MakeDiscreteAssembliesEntryName (Path.ChangeExtension (name, "config"));
					entries.Add ($"{prefix}{mangledName}");
				}
			}
		}
	}

	internal AssemblyStoreExplorer? SelectExplorer (IList<AssemblyStoreExplorer>? explorers, string rid)
	{
		return SelectExplorer (explorers, MonoAndroidHelper.RidToArch (rid));
	}

	internal AssemblyStoreExplorer? SelectExplorer (IList<AssemblyStoreExplorer>? explorers, AndroidTargetArch arch)
	{
		if (explorers == null || explorers.Count == 0) {
			return null;
		}

		// If we don't care about target architecture, we check the first store, since all of them will have the same
		// assemblies. Otherwise, we try to locate the correct store.
		if (arch == AndroidTargetArch.None) {
			return explorers[0];
		}

		foreach (var e in explorers) {
			if (e.TargetArch == null || e.TargetArch != arch) {
				continue;
			}
			return e;
		}

		Console.WriteLine ($"Failed to find assembly store for architecture '{arch}' in archive '{_archivePath}'");
		return null;
	}

	/// <summary>
	/// Takes "old style" `assemblies/assembly.dll` path and returns (if possible) a set of paths that reflect the new
	/// location of `lib/{ARCH}/assembly.dll.so`. A list is returned because, if `arch` is `None`, we'll return all
	/// the possible architectural paths.
	/// An exception is thrown if we cannot transform the path for some reason. It should **not** be handled.
	/// </summary>
    private static List<string>? TransformArchiveAssemblyPath (string path, AndroidTargetArch arch)
	{
		if (string.IsNullOrEmpty (path)) {
			throw new ArgumentException (nameof (path), "must not be null or empty");
		}

		if (!path.StartsWith ("assemblies/", StringComparison.Ordinal)) {
			return new List<string> { path };
		}

		var parts = path.Split ('/');
		if (parts.Length < 2) {
			throw new InvalidOperationException ($"Path '{path}' must consist of at least two segments separated by `/`");
		}

		// We accept:
		//   assemblies/assembly.dll
		//   assemblies/{CULTURE}/assembly.dll
		//   assemblies/{ABI}/assembly.dll
		//   assemblies/{ABI}/{CULTURE}/assembly.dll
		if (parts.Length > 4) {
			throw new InvalidOperationException ($"Path '{path}' must not consist of more than 4 segments separated by `/`");
		}

		string? fileName = null;
		string? culture = null;
		string? abi = null;

		switch (parts.Length) {
			// Full satellite assembly path, with abi
			case 4:
				abi = parts[1];
				culture = parts[2];
				fileName = parts[3];
				break;

			// Assembly path with abi or culture
			case 3:
				// If the middle part isn't a valid abi, we treat it as a culture name
				if (MonoAndroidHelper.IsValidAbi (parts[1])) {
					abi = parts[1];
				} else {
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
		var abis = new List<string> ();
		if (!string.IsNullOrEmpty (abi)) {
			abis.Add (abi);
		} else if (arch == AndroidTargetArch.None) {
			foreach (AndroidTargetArch targetArch in MonoAndroidHelper.SupportedTargetArchitectures) {
				abis.Add (MonoAndroidHelper.ArchToAbi (targetArch));
			}
		} else {
			abis.Add (MonoAndroidHelper.ArchToAbi (arch));
		}

		if (!string.IsNullOrEmpty (culture)) {
			// Android doesn't allow us to put satellite assemblies in lib/{CULTURE}/assembly.dll.so, we must instead
			// mangle the name.
			fileTypeMarker = MonoAndroidHelper.MANGLED_ASSEMBLY_SATELLITE_ASSEMBLY_MARKER;
			fileName = $"{culture}{MonoAndroidHelper.SATELLITE_CULTURE_END_MARKER_CHAR}{fileName}";
		}

		var ret = new List<string> ();
		var newParts = new List<string> {
			string.Empty, // ABI placeholder
			$"{fileTypeMarker}{fileName}.so",
		};

		foreach (var a in abis) {
			newParts[0] = a;
			ret.Add (MonoAndroidHelper.MakeZipArchivePath ("lib", newParts));
		}

		return ret;
	}

	internal static bool ArchiveContains (List<string> archiveContents, string entryPath, AndroidTargetArch arch)
	{
		if (archiveContents.Count == 0) {
			return false;
		}

		var potentialEntries = TransformArchiveAssemblyPath (entryPath, arch);
		if (potentialEntries == null || potentialEntries.Count == 0) {
			return false;
		}

		foreach (var wantedEntry in potentialEntries) {
			Console.WriteLine ($"Wanted entry: {wantedEntry}");
			foreach (var existingEntry in archiveContents) {
				if (string.Compare (existingEntry, wantedEntry, StringComparison.Ordinal) == 0) {
					return true;
				}
			}
		}

		return false;
	}

	/// <summary>
	/// Checks whether <paramref name="entryPath"/> exists in the archive or assembly store.  The path should use the
	/// "old style" `assemblies/{ABI}/assembly.dll` format.
	/// </summary>
	public bool Exists (string entryPath, bool forceRefresh = false, AndroidTargetArch arch = AndroidTargetArch.None)
	{
		return ArchiveContains (ListArchiveContents (_assembliesRootDir, forceRefresh), entryPath, arch);
	}

	public void Contains (ICollection<string> fileNames, out List<string> existingFiles, out List<string> missingFiles, out List<string> additionalFiles, IEnumerable<AndroidTargetArch>? targetArches = null)
	{
		if (fileNames == null) {
			throw new ArgumentNullException (nameof (fileNames));
		}

		if (fileNames.Count == 0) {
			throw new ArgumentException ("must not be empty", nameof (fileNames));
		}

		if (_useAssemblyStores) {
			StoreContains (fileNames, out existingFiles, out missingFiles, out additionalFiles, targetArches);
		} else {
			ArchiveContains (fileNames, out existingFiles, out missingFiles, out additionalFiles, targetArches);
		}
	}

	private List<AndroidTargetArch> GetSupportedArches(IEnumerable<AndroidTargetArch>? runtimeIdentifiers)
	{
		var rids = new List<AndroidTargetArch> ();
		if (runtimeIdentifiers != null) {
			rids.AddRange (runtimeIdentifiers);
		}

		if (rids.Count == 0) {
			rids.AddRange (MonoAndroidHelper.SupportedTargetArchitectures);
		}

		return rids;
	}

	void ListFiles (List<string> existingFiles, List<string> missingFiles, List<string> additionalFiles)
	{
		Console.WriteLine ("Archive contents:");
		ListFiles ("existing files", existingFiles);
		ListFiles ("missing files", missingFiles);
		ListFiles ("additional files", additionalFiles);

		void ListFiles (string label, List<string> list)
		{
			Console.WriteLine ($"  {label}:");
			if (list.Count == 0) {
				Console.WriteLine ("    none");
				return;
			}

			foreach (string file in list) {
				Console.WriteLine ($"    {file}");
			}
		}
	}

	(string prefixAssemblies, string prefixLib) GetArchivePrefixes (string abi) => ($"{MonoAndroidHelper.MakeZipArchivePath (_assembliesRootDir, abi)}/", $"lib/{abi}/");

	internal void ArchiveContains (ICollection<string> fileNames, out List<string> existingFiles, out List<string> missingFiles, out List<string> additionalFiles, IEnumerable<AndroidTargetArch>? targetArches = null)
	{
		using var zip = ZipFile.OpenRead(_archivePath);
		existingFiles = zip.Entries.Where (a => a.FullName.StartsWith (_assembliesRootDir, StringComparison.InvariantCultureIgnoreCase)).Select (a => a.FullName).ToList ();
		existingFiles.AddRange (zip.Entries.Where (a => a.FullName.StartsWith ("lib/", StringComparison.OrdinalIgnoreCase)).Select (a => a.FullName));

		var arches = GetSupportedArches (targetArches);

		missingFiles = [];
		additionalFiles = [];
		foreach (var arch in arches) {
			string abi = MonoAndroidHelper.ArchToAbi (arch);
			missingFiles.AddRange (GetMissingFilesForAbi (abi));
			additionalFiles.AddRange (GetAdditionalFilesForAbi (abi, existingFiles));
		}
		ListFiles (existingFiles, missingFiles, additionalFiles);
        return;

        IEnumerable<string> GetMissingFilesForAbi (string abi)
		{
			var (prefixAssemblies, prefixLib) = GetArchivePrefixes (abi);
			return fileNames.Where (x => {
				string? culture = null;
				var fileName = x;
				var slashIndex = x.IndexOf ('/');
				if (slashIndex > 0) {
					culture = x.Substring (0, slashIndex);
					fileName = x.Substring (slashIndex + 1);
				}

				return !zip.ContainsEntry (MonoAndroidHelper.MakeZipArchivePath (prefixAssemblies, x)) &&
					   !zip.ContainsEntry (MonoAndroidHelper.MakeZipArchivePath (prefixLib, x)) &&
					   !zip.ContainsEntry (MonoAndroidHelper.MakeZipArchivePath (prefixAssemblies, MonoAndroidHelper.MakeDiscreteAssembliesEntryName (fileName, culture))) &&
					   !zip.ContainsEntry (MonoAndroidHelper.MakeZipArchivePath (prefixLib, MonoAndroidHelper.MakeDiscreteAssembliesEntryName (fileName, culture)));
			});
		}

		IEnumerable<string> GetAdditionalFilesForAbi (string abi, List<string> existingFiles)
		{
			var (prefixAssemblies, prefixLib) = GetArchivePrefixes (abi);
			return existingFiles.Where (x => !fileNames.Contains (x.Replace (prefixAssemblies, string.Empty)) && !fileNames.Contains (x.Replace (prefixLib, string.Empty)));
		}
	}

	internal void StoreContains (ICollection<string> fileNames, out List<string> existingFiles, out List<string> missingFiles, out List<string> additionalFiles, IEnumerable<AndroidTargetArch>? targetArches = null)
	{
		var assemblyNames = fileNames.Where (x => x.EndsWith (".dll", StringComparison.OrdinalIgnoreCase)).ToList ();
		var configFiles = fileNames.Where (x => x.EndsWith (".config", StringComparison.OrdinalIgnoreCase)).ToList ();
		var debugFiles = fileNames.Where (x => x.EndsWith (".pdb", StringComparison.OrdinalIgnoreCase)).ToList ();
		var otherFiles = fileNames.Where (x => !SpecialExtensions.Contains (Path.GetExtension (x))).ToList ();

		existingFiles = new List<string> ();
		missingFiles = new List<string> ();
		additionalFiles = new List<string> ();

		using var zip = ZipFile.OpenRead(_archivePath);

		var arches = GetSupportedArches (targetArches);
		var (explorers, errorMessage) = AssemblyStoreExplorer.Open(_archivePath, _logger);

		foreach (var arch in arches) {
			var explorer = SelectExplorer (explorers, arch);
			if (explorer == null) {
				continue;
			}

			if (otherFiles.Count > 0) {
				var (prefixAssemblies, prefixLib) = GetArchivePrefixes (MonoAndroidHelper.ArchToAbi (arch));

				foreach (string file in otherFiles) {
					var fullPath = prefixAssemblies + file;
					if (zip.ContainsEntry (fullPath)) {
						existingFiles.Add (file);
					}

					fullPath = prefixLib + file;
					if (zip.ContainsEntry (fullPath)) {
						existingFiles.Add (file);
					}
				}
			}

            if (explorer.AssembliesByName is not null)
            {
                foreach (var f in explorer.AssembliesByName)
                {
                    Console.WriteLine($"DEBUG!\tKey:{f.Key}");
                }

                if (explorer.AssembliesByName.Count != 0)
                {
                    existingFiles.AddRange(explorer.AssembliesByName.Keys);

                    // We need to fake config and debug files since they have no named entries in the storeReader
                    foreach (var file in configFiles)
                    {
                        var asm = GetStoreAssembly(explorer, file);
                        if (asm == null)
                        {
                            continue;
                        }

                        if (asm.ConfigOffset > 0)
                        {
                            existingFiles.Add(file);
                        }
                    }

                    foreach (string file in debugFiles)
                    {
                        var asm = GetStoreAssembly(explorer, file);
                        if (asm == null)
                        {
                            continue;
                        }

                        if (asm.DebugOffset > 0)
                        {
                            existingFiles.Add(file);
                        }
                    }
                }
            }
        }

		foreach (string file in fileNames) {
			if (existingFiles.Contains (file)) {
				continue;
			}
			missingFiles.Add (file);
		}

		additionalFiles = existingFiles.Where (x => !fileNames.Contains (x)).ToList ();
		ListFiles (existingFiles, missingFiles, additionalFiles);
        return;

        AssemblyStoreItem? GetStoreAssembly (AssemblyStoreExplorer explorer, string file)
		{
			var assemblyName = Path.GetFileNameWithoutExtension (file);
			return explorer.AssembliesByName?.TryGetValue(assemblyName, out var asm) is true
                ? asm
                : null;
		}
	}
}
