using System;

namespace Sentry.Internal
{
    internal static class EnvironmentLocator
    {
        private static readonly Lazy<string?> Environment = new(Locate);

        /// <summary>
        /// Attempts to locate the environment the app is running in.
        /// </summary>
        /// <returns>The Environment name or null, if it couldn't be located.</returns>
        public static string? Current => Environment.Value;

        internal static string? Locate() => System.Environment.GetEnvironmentVariable(Constants.EnvironmentEnvironmentVariable);
    }
}
