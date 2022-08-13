using System;

namespace Sentry.Internal
{
    internal static class DistributionLocator
    {
        private static Lazy<string?> FromEnvironmentLazy = new(LocateFromEnvironment);

        // For testing
        internal static void Reset() => FromEnvironmentLazy = new(LocateFromEnvironment);

        // Internal for testing
        internal static string? LocateFromEnvironment() =>
            Environment.GetEnvironmentVariable(Constants.DistributionEnvironmentVariable);
            // ?? TODO: Default value?

        public static string? Resolve(SentryOptions options) => options.Distribution ?? FromEnvironmentLazy.Value;
    }
}
