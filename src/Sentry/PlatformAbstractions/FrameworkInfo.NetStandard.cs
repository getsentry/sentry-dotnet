#if !NETFX

using System.Collections.Generic;
using System.Linq;

namespace Sentry.PlatformAbstractions
{
    /// <summary>
    /// No-op version for netstandard targets
    /// </summary>
    public static partial class FrameworkInfo
    {
        /// <summary>
        /// No-op version for netstandard targets
        /// </summary>
        /// <param name="clr"></param>
        /// <returns></returns>
        public static FrameworkInstallation? GetLatest(int clr) => null;

        /// <summary>
        /// No-op version for netstandard targets
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<FrameworkInstallation> GetInstallations()
            => Enumerable.Empty<FrameworkInstallation>();
    }
}

#endif
