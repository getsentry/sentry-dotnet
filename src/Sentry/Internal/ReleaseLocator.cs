using System;

namespace Sentry.Internal
{
    internal static class ReleaseLocator
    {
        private static readonly Lazy<string?> FromEnvironmentLazy = new(ResolveFromEnvironment);

        // Internal for testing
        internal static string? ResolveFromEnvironment() =>
            Environment.GetEnvironmentVariable(Constants.ReleaseEnvironmentVariable)
            ?? ApplicationVersionLocator.GetCurrent();

        public static string? Resolve(SentryOptions options) => options.Release ?? FromEnvironmentLazy.Value;
    }
}
