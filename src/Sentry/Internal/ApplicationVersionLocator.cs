using System.Reflection;
using Sentry.Reflection;

namespace Sentry.Internal
{
    internal static class ApplicationVersionLocator
    {
        public static string? GetCurrent() => GetCurrent(Assembly.GetEntryAssembly());

        internal static string? GetCurrent(Assembly? asm)
        {
            if (asm is null)
            {
                return null;
            }

            var name = asm.GetName().Name;
            var version = asm.GetVersion();
            if (string.IsNullOrWhiteSpace(name) ||
                string.IsNullOrWhiteSpace(version))
            {
                return null;
            }

            // Don't add name prefix if it's already set by the user
            if (version.Contains('@'))
            {
                return version;
            }

            return $"{name}@{version}";

        }
    }
}
