/*
 * Adapted from https://github.com/dotnet/android/blob/3822f2b1ee7061813b1d456af22e043e66e2f698/src/Xamarin.Android.Build.Tasks/Utilities/MonoAndroidHelper.Basic.cs
 * Original code licensed under the MIT License (https://github.com/dotnet/android/blob/5ebcb1dd1503648391e3c0548200495f634d90c6/LICENSE.TXT)
 */

namespace Sentry.Android.AssemblyReader.V2;

internal static class MonoAndroidHelper
{
    private static class AndroidAbi
    {
        public const string Arm32 = "armeabi-v7a";
        public const string Arm64 = "arm64-v8a";
        public const string X86 = "x86";
        public const string X64 = "x86_64";
    }

    private static class RuntimeIdentifier
    {
        public const string Arm32 = "android-arm";
        public const string Arm64 = "android-arm64";
        public const string X86 = "android-x86";
        public const string X64 = "android-x64";
    }

    public static readonly HashSet<AndroidTargetArch> SupportedTargetArchitectures =
    [
        AndroidTargetArch.Arm,
        AndroidTargetArch.Arm64,
        AndroidTargetArch.X86,
        AndroidTargetArch.X86_64
    ];
    private static readonly char[] ZipPathTrimmedChars = { '/', '\\' };
    private static readonly Dictionary<string, string> AbiToRidMap = new(StringComparer.OrdinalIgnoreCase) {
        { AndroidAbi.Arm32, RuntimeIdentifier.Arm32 },
        { AndroidAbi.Arm64, RuntimeIdentifier.Arm64 },
        { AndroidAbi.X86,   RuntimeIdentifier.X86 },
        { AndroidAbi.X64,   RuntimeIdentifier.X64 },
    };
    private static readonly Dictionary<AndroidTargetArch, string> ArchToAbiMap = new Dictionary<AndroidTargetArch, string> {
        { AndroidTargetArch.Arm,    AndroidAbi.Arm32 },
        { AndroidTargetArch.Arm64,  AndroidAbi.Arm64 },
        { AndroidTargetArch.X86,    AndroidAbi.X86 },
        { AndroidTargetArch.X86_64, AndroidAbi.X64 },
    };

    public static string ArchToAbi(AndroidTargetArch arch)
    {
        if (!ArchToAbiMap.TryGetValue(arch, out var abi))
        {
            throw new InvalidOperationException($"Internal error: unsupported architecture '{arch}'");
        }

        return abi;
    }

    public static bool IsValidAbi(string abi) => AbiToRidMap.ContainsKey(abi);

    public static string MakeZipArchivePath(string part1, ICollection<string>? pathParts)
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(part1))
        {
            parts.Add(part1.TrimEnd(ZipPathTrimmedChars));
        }

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
}
