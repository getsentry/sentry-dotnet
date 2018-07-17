using System;

namespace Sentry.Internal
{
    internal static class EnvironmentLocator
    {
        /// <summary>
        /// Attempts to locate the environment the app is running in
        /// </summary>
        /// <returns>The Environment name or null, if it couldn't be located.</returns>
        public static string GetCurrent()
            => Environment.GetEnvironmentVariable(Constants.EnvironmentEnvironmentVariable);
    }
}
