using System;

namespace Sentry.PlatformAbstractions
{
    /// <summary>
    /// A .NET Framework installation
    /// </summary>
    /// <seealso href="https://en.wikipedia.org/wiki/.NET_Framework_version_history"/>
    /// <seealso href="https://docs.microsoft.com/en-us/dotnet/framework/migration-guide/how-to-determine-which-versions-are-installed"/>
    public class FrameworkInstallation
    {
        /// <summary>
        /// Short name
        /// </summary>
        /// <example>
        /// v2.0.50727, v3.5, v4.0
        /// </example>
        public string? ShortName { get; set; }
        /// <summary>
        /// Version
        /// </summary>
        /// <example>
        /// 2.0.50727.4927, 3.0.30729.4926, 3.5.30729.4926
        /// </example>
        public Version? Version { get; set; }
        /// <summary>
        /// Service pack number, if any
        /// </summary>
        /// <remarks>
        /// Only relevant prior to .NET 4
        /// </remarks>
        public int? ServicePack { get; set; }
        /// <summary>
        /// Type of Framework profile
        /// </summary>
        /// <remarks>Only relevant for .NET 3.5 and 4.0</remarks>
        /// <seealso href="https://docs.microsoft.com/en-us/dotnet/framework/deployment/client-profile"/>
        public FrameworkProfile? Profile { get; set; }
        /// <summary>
        ///  A .NET Framework release key
        /// </summary>
        /// <remarks>
        /// Windows registry key:
        /// HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\Release
        /// Only applicable when on Windows, with full .NET Framework 4.5 and later.
        /// </remarks>
        /// <see href="https://docs.microsoft.com/en-us/dotnet/framework/migration-guide/how-to-determine-which-versions-are-installed"/>
        public int? Release { get; set; }

        /// <inheritdoc />
        public override string ToString()
        {
            return Version?.Build > 0
                ? $"{Version.Major}.{Version.Minor}.{Version.Build}"
                : $"{Version?.Major ?? 0}.{Version?.Minor ?? 0}";
        }
    }

    /// <summary>
    /// Type of Framework profile
    /// </summary>
    /// <remarks>Only relevant for .NET 3.5 and 4.0</remarks>
    /// <seealso href="https://docs.microsoft.com/en-us/dotnet/framework/deployment/client-profile"/>
    public enum FrameworkProfile
    {
        /// <summary>
        /// The .NET Client Profile is a subset of the .NET Framework
        /// </summary>
        Client,
        /// <summary>
        /// The full .NET Framework
        /// </summary>
        Full
    }
}
