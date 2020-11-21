using System;

namespace Sentry.Internal
{
    internal static class ReleaseLocator
    {
        /// <summary>
        /// Attempts to locate the application release
        /// </summary>
        /// <returns>The app release or null, if it couldn't be located.</returns>
        public static string? GetCurrent()
            => Environment.GetEnvironmentVariable(Constants.ReleaseEnvironmentVariable)
                ?? ApplicationVersionLocator.GetCurrent();
    }
}
