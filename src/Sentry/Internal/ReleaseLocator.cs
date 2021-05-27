using System;

namespace Sentry.Internal
{
    internal static class ReleaseLocator
    {
        private static readonly Lazy<string?> CurrentLazy = new(() =>
            Environment.GetEnvironmentVariable(Constants.ReleaseEnvironmentVariable)
            ?? ApplicationVersionLocator.GetCurrent()
        );

        public static string? Resolve(SentryOptions options) => options.Release ?? CurrentLazy.Value;
    }
}
