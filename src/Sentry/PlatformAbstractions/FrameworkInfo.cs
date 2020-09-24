using System.Collections.Generic;

namespace Sentry.PlatformAbstractions
{
    /// <summary>
    /// Information about .NET Framework in the running machine
    /// The purpose of this partial class is to expose the API to all targets
    /// For netstandard, the call to methods will be a simple no-op.
    /// </summary>
    public static partial class FrameworkInfo
    {
        /// <summary>
        /// The map between release number and version number
        /// </summary>
        /// <see href="https://docs.microsoft.com/en-us/dotnet/framework/migration-guide/how-to-determine-which-versions-are-installed" />
        public static IReadOnlyDictionary<int, string> NetFxReleaseVersionMap { get; }
            = new Dictionary<int, string>
            {
                {378389, "4.5"},
                {378675, "4.5.1"},
                {378758, "4.5.1"},
                {379893, "4.5.2"},
                {393295, "4.6"},
                {393297, "4.6"},
                {394254, "4.6.1"},
                {394271, "4.6.1"},
                {394802, "4.6.2"},
                {394806, "4.6.2"},
                {460798, "4.7"},
                {460805, "4.7"},
                {461308, "4.7.1"},
                {461310, "4.7.1"},
                {461808, "4.7.2"},
                {461814, "4.7.2"},
                {528040, "4.8"},
                {528049, "4.8"},
                {528209, "4.8"},
                {528372, "4.8"},
            };
    }
}
