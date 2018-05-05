#if NETFX
using System;

namespace Sentry.PlatformAbstractions
{
    public class FrameworkInstallation
    {
        // v2.0.50727, v3.5, v4.0
        public string ShortName { get; set; }
        /// <summary>
        /// Version
        /// </summary>
        /// <example>
        /// 2.0.50727.4927, 3.0.30729.4926, 3.5.30729.4926
        /// </example>
        public Version Version { get; set; }
        // Only relevant prior to .NET 4
        public int? ServicePack { get; set; }
        // Client or Full (Only relevant for .NET 4.0)
        // https://docs.microsoft.com/en-us/dotnet/framework/deployment/client-profile
        public FrameworkProfile? Profile { get; set; }
        // .NET Framework 4.5+ release number
        public int? Release { get; set; }

        public override string ToString()
        {
            return Version.Build > 0
                ? $"{Version.Major}.{Version.Minor}.{Version.Build}"
                : $"{Version.Major}.{Version.Minor}";
        }
    }

    public enum FrameworkProfile
    {
        Client,
        Full
    }
}
#endif
