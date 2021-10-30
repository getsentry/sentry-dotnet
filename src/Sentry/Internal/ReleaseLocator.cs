using System;

namespace Sentry.Internal
{
    internal static class ReleaseLocator
    {
        internal static Lazy<string?> FromEnvironmentLazy = new(LocateFromEnvironment);

        // Internal for testing
        internal static string? LocateFromEnvironment() =>
            Environment.GetEnvironmentVariable(Constants.ReleaseEnvironmentVariable)
            ?? ApplicationVersionLocator.GetCurrent();

        public static string? Resolve(SentryOptions options) => options.Release ?? FromEnvironmentLazy.Value;
    }
}
