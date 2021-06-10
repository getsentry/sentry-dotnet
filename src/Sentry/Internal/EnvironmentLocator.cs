using System;
using System.Diagnostics;

namespace Sentry.Internal
{
    internal static class EnvironmentLocator
    {
        private static Lazy<string?> FromEnvironmentVariableLazy = new(LocateFromEnvironmentVariable);

        // For testing
        internal static void Reset() => FromEnvironmentVariableLazy = new(LocateFromEnvironmentVariable);

        internal static string? LocateFromEnvironmentVariable() =>
            Environment.GetEnvironmentVariable(Constants.EnvironmentEnvironmentVariable);

        public static string Resolve(SentryOptions options)
        {
            var fromEnvironmentVariable = FromEnvironmentVariableLazy.Value;
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
