#if NETFRAMEWORK
using System;
using System.Collections.Generic;
using Microsoft.Win32;
using Sentry.Extensibility;

namespace Sentry.PlatformAbstractions
{
    /// <summary>
    /// Information about .NET Framework in the running machine
    /// </summary>
    public static partial class FrameworkInfo
    {
        private const string NetFxNdpRegistryKey = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\";
        private const string NetFxNdpFullRegistryKey = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\";

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
        /// <returns>The framework installation or null if none is found.</returns>
        public static FrameworkInstallation? GetLatest(int clrVersion)
        {
            // CLR versions
            // https://docs.microsoft.com/en-us/dotnet/standard/clr
            if (clrVersion != 1 && clrVersion != 2 && clrVersion != 4)
            {
                return null;
            }

            if (clrVersion == 4)
            {
                var release = Get45PlusLatestInstallationFromRegistry();
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
                    if (installation.Version?.Major is 2 or 3 && installation.Version >= latest.Version)
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
                    if (installation.Version?.Major == 4 && installation.Version >= latest.Version)
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
        /// <seealso href="https://docs.microsoft.com/en-us/dotnet/framework/migration-guide/how-to-determine-which-versions-are-installed#to-find-net-framework-versions-by-querying-the-registry-in-code-net-framework-1-4"/>
        /// <returns>Enumeration of installations</returns>
        public static IEnumerable<FrameworkInstallation> GetInstallations()
        {
            using var ndpKey = Registry.LocalMachine.OpenSubKey(NetFxNdpRegistryKey, false);
            if (ndpKey == null)
            {
                yield break;
            }

            foreach (var versionKeyName in ndpKey.GetSubKeyNames())
            {
                if (!versionKeyName.StartsWith("v") || ndpKey.OpenSubKey(versionKeyName) is not { } versionKey)
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
            Version? version = null;

            var release = subKey.GetInt("Release");
            if (release != null)
            {
                // 4.5+
                var displayableVersion = GetNetFxVersionFromRelease(release.Value);
                if (displayableVersion != null)
                {
                    version = displayableVersion;
                }
            }

            if (version == null)
            {
                var input = subKey.GetString("Version");
                if (input != null && Version.TryParse(input, out var parsed))
                {
                    version = parsed;
                }
            }

            var servicePack = subKey.GetInt("SP");

            var profile = subKeyName switch
            {
                "Full" => FrameworkProfile.Full,
                "Client" => FrameworkProfile.Client,
                _ => (FrameworkProfile?)null
            };

            return new FrameworkInstallation
            {
                Release = release,
                Version = version,
                ServicePack = servicePack,
                Profile = profile
            };
        }

        // https://docs.microsoft.com/en-us/dotnet/framework/migration-guide/how-to-determine-which-versions-are-installed#to-find-net-framework-versions-by-querying-the-registry-in-code-net-framework-45-and-later
        private static int? Get45PlusLatestInstallationFromRegistry()
        {
            using var ndpKey = Registry.LocalMachine.OpenSubKey(NetFxNdpFullRegistryKey, false);
            return ndpKey?.GetInt("Release");
        }

        private static Version? GetNetFxVersionFromRelease(int release)
        {
            if (NetFxReleaseVersionMap.TryGetValue(release, out var version))
            {
                return Version.Parse(version);
            }

            SentrySdk.CurrentOptions?.LogWarning("Could not determine .NET Framework version for release {0}.", release);
            return null;
        }
    }
}

#endif
