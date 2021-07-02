#if NET461
using System;
using System.Collections.Generic;
using Microsoft.Win32;

namespace Sentry.PlatformAbstractions
{
    /// <summary>
    /// Information about .NET Framework in the running machine
    /// </summary>
    public static partial class FrameworkInfo
    {
        /// <summary>
        /// Blocks any function to access the registry if a previous attempt had failed.
        /// By default: it's set to false.
        /// </summary>
        internal static bool RegistryLocked { get; private set; }

        /// <summary>
        /// Get the latest Framework installation for the specified CLR
        /// </summary>
        /// <remarks>
        /// Supports the current 3 CLR versions:
        /// CLR 1 => .NET 1.0, 1.1
        /// CLR 2 => .NET 2.0, 3.0, 3.5
        /// CLR 4 => .NET 4.0, 4.5.x, 4.6.x, 4.7.x
        /// </remarks>
        /// <param name="clrVersion">The CLR version: 1, 2 or 4</param>
        /// <param name="registryLock">Blocks this function to access the System's registry.</param>
        /// <returns>The framework installation or null if none is found, used for fine-tuning the Latest info.</returns>
        public static FrameworkInstallation? GetLatest(int clrVersion, bool registryLock = false)
        {
            // CLR versions
            // https://docs.microsoft.com/en-us/dotnet/standard/clr
            if (clrVersion != 1 && clrVersion != 2 && clrVersion != 4)
            {
                return null;
            }

            if (clrVersion == 4)
            {
                int? release = null;
                try
                {
                    if (registryLock)
                    {
                        RegistryLocked = true;
                    }
                    release = Get45PlusLatestInstallationFromRegistry(RegistryLocked);
                }
                catch (Exception ex)
                {
                    _ = ex;
                    //Do something?
                    RegistryLocked = true;
                }
                if (release != null)
                {
                    return new FrameworkInstallation
                    {
                        Version = GetNetFxVersionFromRelease(release.Value),
                        Release = release
                    };
                }
            }

            FrameworkInstallation latest = null;
            foreach (var installation in GetInstallations())
            {
                latest ??= installation;

                if (clrVersion == 2)
                {
                    // CLR 2 runs .NET 2 to 3.5
                    if ((installation.Version.Major == 2 || installation.Version.Major == 3)
                        && installation.Version >= latest.Version)
                    {
                        latest = installation;
                    }
                    else
                    {
                        break;
                    }
                }
                else if (clrVersion == 4)
                {
                    if (installation.Version.Major == 4
                        && installation.Version >= latest.Version)
                    {
                        latest = installation;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return latest;
        }

        /// <summary>
        /// Get all .NET Framework installations in this machine
        /// </summary>
        /// <param name="registryLock">Blocks this function to access the System's registry.</param>
        /// <seealso href="https://docs.microsoft.com/en-us/dotnet/framework/migration-guide/how-to-determine-which-versions-are-installed#to-find-net-framework-versions-by-querying-the-registry-in-code-net-framework-1-4"/>
        /// <returns>Enumeration of installations</returns>
        public static IEnumerable<FrameworkInstallation> GetInstallations(bool registryLock = false)
        {
            if (registryLock || RegistryLocked)
            {
                yield break;
            }
            using var ndpKey = RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, string.Empty)
                .OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\");
            if (ndpKey == null)
            {
                yield break;
            }

            foreach (var versionKeyName in ndpKey.GetSubKeyNames())
            {
                if (!versionKeyName.StartsWith("v") || !(ndpKey.OpenSubKey(versionKeyName) is { } versionKey))
                {
                    continue;
                }

                var version = versionKey.GetString("Version");
                if (version != null && versionKey.GetInt("Install") == 1)
                {
                    // 1.0 to 3.5
                    _ = Version.TryParse(version, out var parsed);
                    yield return new FrameworkInstallation
                    {
                        ShortName = versionKeyName,
                        Version = parsed,
                        ServicePack = versionKey.GetInt("SP")
                    };

                    continue;
                }

                // 4.0+
                foreach (var subKeyName in versionKey.GetSubKeyNames())
                {
                    var subKey = versionKey.OpenSubKey(subKeyName);
                    if (subKey?.GetInt("Install") != 1)
                    {
                        continue;
                    }

                    yield return GetFromV4(subKey, subKeyName);
                }
            }
        }

        private static FrameworkInstallation GetFromV4(RegistryKey subKey, string subKeyName)
        {
            var hasRelease = int.TryParse(
                subKey.GetValue("Release", null)?.ToString(), out var release);

            Version version = null;
            if (hasRelease)
            {
                // 4.5+
                var displayableVersion = GetNetFxVersionFromRelease(release);
                if (displayableVersion != null)
                {
                    version = displayableVersion;
                }
            }

            if (version == null)
            {
                _ = Version.TryParse(subKey.GetString("Version"), out var parsed);
                version = parsed;
            }

            return new FrameworkInstallation
            {
                Profile = subKeyName switch
                {
                    "Full" => FrameworkProfile.Full,
                    "Client" => FrameworkProfile.Client,
                    _ => null
                },
                Version = version,
                ServicePack = subKey.GetInt("SP"),
                Release = hasRelease ? release : null
            };
        }

        // https://docs.microsoft.com/en-us/dotnet/framework/migration-guide/how-to-determine-which-versions-are-installed#to-find-net-framework-versions-by-querying-the-registry-in-code-net-framework-45-and-later
        internal static int? Get45PlusLatestInstallationFromRegistry(bool registryLock)
        {
            if (registryLock)
            {
                return null;
            }
            using var ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32)
                .OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\");
            return ndpKey?.GetInt("Release");
        }

        internal static Version GetNetFxVersionFromRelease(int release)
        {
            _ = NetFxReleaseVersionMap.TryGetValue(release, out var version);
            _ = Version.TryParse(version, out var parsed);
            return parsed;
        }
    }
}

#endif
