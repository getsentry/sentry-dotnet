using System.Reflection;
using Sentry.Reflection;

namespace Sentry.Internal
{
    internal static class ApplicationVersionLocator
    {
        public static string? GetCurrent() => GetCurrent(Assembly.GetEntryAssembly());

        internal static string? GetCurrent(Assembly? asm)
        {
            var version = asm?.GetNameAndVersion().Version;

            return !string.IsNullOrEmpty(version)
                   // If it really was on of the following, app would need to be set explicitly since these are defaults.
                   && version != "0.0.0"
                   && version != "1.0.0"
                   && version != "0.0.0.0"
                   && version != "1.0.0.0"
                ? version
                : null;
        }
    }
}
