using System;
using System.Diagnostics;

namespace Sentry.Internal
{
    internal static class EnvironmentLocator
    {
        private static readonly Lazy<string?> FromEnvironmentVariable = new(LocateFromEnvironmentVariable);

        /// <summary>
        /// Attempts to locate the environment the app is running in.
        /// </summary>
        /// <returns>The Environment name or null, if it couldn't be located.</returns>
        public static string? Current => FromEnvironmentVariable.Value;

        internal static string? LocateFromEnvironmentVariable() =>
            Environment.GetEnvironmentVariable(Constants.EnvironmentEnvironmentVariable);

        public static string Resolve(SentryOptions options)
        {
            // Changing from `LocateFromEnvironmentVariable()` to `Current` fails tests?
            var fromEnvironmentVariable = LocateFromEnvironmentVariable();
            if (!string.IsNullOrWhiteSpace(fromEnvironmentVariable))
            {
                return fromEnvironmentVariable;
            }

            var fromOptions = options.Environment;
            if (!string.IsNullOrWhiteSpace(fromOptions))
            {
                return fromOptions;
            }

            return Debugger.IsAttached
                ? Constants.DebugEnvironmentSetting
                : Constants.ProductionEnvironmentSetting;
        }
    }
}
