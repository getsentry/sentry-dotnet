using System.Collections.Generic;

namespace Sentry.PlatformAbstractions
{
    public class RuntimeInfoOptions
    {
        /// <summary>
        /// Mapping between .NET Framework release number and version
        /// </summary>
        /// <see href="https://docs.microsoft.com/en-us/dotnet/framework/migration-guide/how-to-determine-which-versions-are-installed#to-find-net-framework-versions-by-viewing-the-registry-net-framework-45-and-later"/>
#if HAS_READONLY_COLLECTION
        public IReadOnlyDictionary<int, string> NetFxReleaseVersionMap { get; }
#else
        public IDictionary<int, string> NetFxReleaseVersionMap { get; }
#endif

        public string RuntimeParseRegex { get; }

        public RuntimeInfoOptions(
            string runtimeParseRegex = null,
#if HAS_READONLY_COLLECTION
            IReadOnlyDictionary<int, string> netFxReleaseVersionMap = null)
#else
            IDictionary<int, string> netFxReleaseVersionMap = null)
#endif
        {
            RuntimeParseRegex = runtimeParseRegex ?? "^(?<name>[^\\d]+)(?<version>[\\d+\\.]+[^\\s]+)";
            NetFxReleaseVersionMap = netFxReleaseVersionMap ?? new Dictionary<int, string>
            {
                { 378389, "4.5" },
                { 378675, "4.5.1" },
                { 378758, "4.5.1" },
                { 379893, "4.5.2" },
                { 393295, "4.6" },
                { 393297, "4.6" },
                { 394254, "4.6.1" },
                { 394271, "4.6.1" },
                { 394802, "4.6.2" },
                { 394806, "4.6.2" },
                { 460798, "4.7" },
                { 460805, "4.7" },
                { 461308, "4.7.1" },
                { 461310, "4.7.1" },
                { 461808, "4.7.2" },
                { 461814, "4.7.2" },
            };
        }
    }
}
