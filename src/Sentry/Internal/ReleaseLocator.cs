using System;

namespace Sentry.Internal
{
    internal static class ReleaseLocator
    {
        private static readonly Lazy<string?> CurrentLazy = new(ResolveFromEnvironment);

        private static string? ResolveFromEnvironment() =>
            Environment.GetEnvironmentVariable(Constants.ReleaseEnvironmentVariable)
            ?? ApplicationVersionLocator.GetCurrent();

        // Replacing `ResolveFromEnvironment()` with `CurrentLazy.Value` fails tests?
        public static string? Resolve(SentryOptions options) => options.Release ?? ResolveFromEnvironment();
    }
}
