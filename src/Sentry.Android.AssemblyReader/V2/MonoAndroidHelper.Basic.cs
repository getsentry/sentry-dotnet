/*
 * Adapted from https://github.com/dotnet/android/blob/3822f2b1ee7061813b1d456af22e043e66e2f698/src/Xamarin.Android.Build.Tasks/Utilities/MonoAndroidHelper.Basic.cs
 * Original code licensed under the MIT License (https://github.com/dotnet/android/blob/5ebcb1dd1503648391e3c0548200495f634d90c6/LICENSE.TXT)
 */

namespace Sentry.Android.AssemblyReader.V2;

internal partial class MonoAndroidHelper
{
    public static class AndroidAbi
    {
        public const string Arm32 = "armeabi-v7a";
        public const string Arm64 = "arm64-v8a";
        public const string X86 = "x86";
        public const string X64 = "x86_64";
    }

    public static class RuntimeIdentifier
    {
        public const string Arm32 = "android-arm";
        public const string Arm64 = "android-arm64";
        public const string X86 = "android-x86";
        public const string X64 = "android-x64";
    }

    public static readonly HashSet<AndroidTargetArch> SupportedTargetArchitectures = new HashSet<AndroidTargetArch> {
        AndroidTargetArch.Arm,
        AndroidTargetArch.Arm64,
        AndroidTargetArch.X86,
        AndroidTargetArch.X86_64,
    };
    private static readonly char[] ZipPathTrimmedChars = { '/', '\\' };
    private static readonly Dictionary<string, string> ClangAbiMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
        {"arm64-v8a",   "aarch64"},
        {"armeabi-v7a", "arm"},
        {"x86",         "i686"},
        {"x86_64",      "x86_64"}
    };
    private static readonly Dictionary<string, AndroidTargetArch> AbiToArchMap = new Dictionary<string, AndroidTargetArch>(StringComparer.OrdinalIgnoreCase) {
        { AndroidAbi.Arm32, AndroidTargetArch.Arm },
        { AndroidAbi.Arm64, AndroidTargetArch.Arm64 },
        { AndroidAbi.X86,   AndroidTargetArch.X86 },
        { AndroidAbi.X64,   AndroidTargetArch.X86_64 },
    };
    private static readonly Dictionary<string, string> AbiToRidMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
        { AndroidAbi.Arm32, RuntimeIdentifier.Arm32 },
        { AndroidAbi.Arm64, RuntimeIdentifier.Arm64 },
        { AndroidAbi.X86,   RuntimeIdentifier.X86 },
        { AndroidAbi.X64,   RuntimeIdentifier.X64 },
    };
    private static readonly Dictionary<string, string> RidToAbiMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
        { RuntimeIdentifier.Arm32, AndroidAbi.Arm32 },
        { RuntimeIdentifier.Arm64, AndroidAbi.Arm64 },
        { RuntimeIdentifier.X86,   AndroidAbi.X86 },
        { RuntimeIdentifier.X64,   AndroidAbi.X64 },
    };
    private static readonly Dictionary<string, AndroidTargetArch> RidToArchMap = new Dictionary<string, AndroidTargetArch>(StringComparer.OrdinalIgnoreCase) {
        { RuntimeIdentifier.Arm32, AndroidTargetArch.Arm },
        { RuntimeIdentifier.Arm64, AndroidTargetArch.Arm64 },
        { RuntimeIdentifier.X86,   AndroidTargetArch.X86 },
        { RuntimeIdentifier.X64,   AndroidTargetArch.X86_64 },
    };
    private static readonly Dictionary<AndroidTargetArch, string> ArchToRidMap = new Dictionary<AndroidTargetArch, string> {
        { AndroidTargetArch.Arm,    RuntimeIdentifier.Arm32 },
        { AndroidTargetArch.Arm64,  RuntimeIdentifier.Arm64 },
        { AndroidTargetArch.X86,    RuntimeIdentifier.X86 },
        { AndroidTargetArch.X86_64, RuntimeIdentifier.X64 },
    };
    private static readonly Dictionary<AndroidTargetArch, string> ArchToAbiMap = new Dictionary<AndroidTargetArch, string> {
        { AndroidTargetArch.Arm,    AndroidAbi.Arm32 },
        { AndroidTargetArch.Arm64,  AndroidAbi.Arm64 },
        { AndroidTargetArch.X86,    AndroidAbi.X86 },
        { AndroidTargetArch.X86_64, AndroidAbi.X64 },
    };

    public static AndroidTargetArch AbiToTargetArch(string abi)
    {
        if (!AbiToArchMap.TryGetValue(abi, out AndroidTargetArch arch))
        {
            throw new NotSupportedException($"Internal error: unsupported ABI '{abi}'");
        }
        ;

        return arch;
    }

    public static string AbiToRid(string abi)
    {
        if (!AbiToRidMap.TryGetValue(abi, out var rid))
        {
            throw new NotSupportedException($"Internal error: unsupported ABI '{abi}'");
        }
        ;

        return rid;
    }

    public static string RidToAbi(string rid)
    {
        if (!RidToAbiMap.TryGetValue(rid, out var abi))
        {
            throw new NotSupportedException($"Internal error: unsupported Runtime Identifier '{rid}'");
        }
        ;

        return abi;
    }

    public static AndroidTargetArch RidToArchMaybe(string rid)
    {
        if (!RidToArchMap.TryGetValue(rid, out AndroidTargetArch arch))
        {
            return AndroidTargetArch.None;
        }
        ;

        return arch;
    }

    public static AndroidTargetArch RidToArch(string rid)
    {
        AndroidTargetArch arch = RidToArchMaybe(rid);
        if (arch == AndroidTargetArch.None)
        {
            throw new NotSupportedException($"Internal error: unsupported Runtime Identifier '{rid}'");
        }
        ;

        return arch;
    }

    public static string ArchToRid(AndroidTargetArch arch)
    {
        if (!ArchToRidMap.TryGetValue(arch, out var rid))
        {
            throw new InvalidOperationException($"Internal error: unsupported architecture '{arch}'");
        }
        ;

        return rid;
    }

    public static string ArchToAbi(AndroidTargetArch arch)
    {
        if (!ArchToAbiMap.TryGetValue(arch, out var abi))
        {
            throw new InvalidOperationException($"Internal error: unsupported architecture '{arch}'");
        }
        ;

        return abi;
    }

    public static bool IsValidAbi(string abi) => AbiToRidMap.ContainsKey(abi);
    public static bool IsValidRID(string rid) => RidToAbiMap.ContainsKey(rid);

    public static string? CultureInvariantToString(object? obj)
    {
        if (obj == null)
        {
            return null;
        }

        return Convert.ToString(obj, CultureInfo.InvariantCulture);
    }

    public static string? MapAndroidAbiToClang(string androidAbi)
    {
        if (ClangAbiMap.TryGetValue(androidAbi, out var clangAbi))
        {
            return clangAbi;
        }
        return null;
    }

    public static string MakeZipArchivePath(string part1, params string[]? pathParts)
    {
        return MakeZipArchivePath(part1, (ICollection<string>?)pathParts);
    }

    public static string MakeZipArchivePath(string part1, ICollection<string>? pathParts)
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(part1))
        {
            parts.Add(part1.TrimEnd(ZipPathTrimmedChars));
        }
        ;

        if (pathParts != null && pathParts.Count > 0)
        {
            foreach (string p in pathParts)
            {
                if (string.IsNullOrEmpty(p))
                {
                    continue;
                }
                parts.Add(p.TrimEnd(ZipPathTrimmedChars));
            }
        }

        if (parts.Count == 0)
        {
            return string.Empty;
        }

        return string.Join("/", parts);
    }

    // These 3 MUST be the same as the like-named constants in src/monodroid/jni/shared-constants.hh
    public const string MANGLED_ASSEMBLY_NAME_EXT = ".so";
    public const string MANGLED_ASSEMBLY_REGULAR_ASSEMBLY_MARKER = "lib_";
    public const string MANGLED_ASSEMBLY_SATELLITE_ASSEMBLY_MARKER = "lib-";
    public const string SATELLITE_CULTURE_END_MARKER_CHAR = "_";

    /// <summary>
    /// Mangles APK/AAB entry name for assembly and their associated pdb and config entries in the
    /// way expected by our native runtime.  Must **NOT** be used to mangle names when assembly stores
    /// are used.  Must **NOT** be used for entries other than assemblies and their associated files.
    /// </summary>
    public static string MakeDiscreteAssembliesEntryName(string name, string? culture = null)
    {
        if (!string.IsNullOrEmpty(culture))
        {
            return $"{MANGLED_ASSEMBLY_SATELLITE_ASSEMBLY_MARKER}{culture}_{name}{MANGLED_ASSEMBLY_NAME_EXT}";
        }

        return $"{MANGLED_ASSEMBLY_REGULAR_ASSEMBLY_MARKER}{name}{MANGLED_ASSEMBLY_NAME_EXT}";
    }

    /// <summary>
    /// Returns size of the extension + length of the prefix for mangled assembly names. This is
    /// used to pre-allocate space for assembly names in `libxamarin-app.so`
    /// <seealso cref="MakeDiscreteAssembliesEntryName"/>
    /// </summary>
    public static ulong GetMangledAssemblyNameSizeOverhead()
    {
        // Satellite marker is one character more, for the `-` closing the culture part
        return (ulong)MANGLED_ASSEMBLY_NAME_EXT.Length +
               (ulong)Math.Max(MANGLED_ASSEMBLY_SATELLITE_ASSEMBLY_MARKER.Length + 1, MANGLED_ASSEMBLY_REGULAR_ASSEMBLY_MARKER.Length);
    }

    public static byte[] Utf8StringToBytes(string str) => Encoding.UTF8.GetBytes(str);
    public static byte[] Utf16StringToBytes(string str) => Encoding.Unicode.GetBytes(str);
}
