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

            var nameAndVersion = asm.GetNameAndVersion();

            if (string.IsNullOrWhiteSpace(nameAndVersion.Name) ||
                string.IsNullOrWhiteSpace(nameAndVersion.Version))
            {
                return null;
            }

            // Don't add name prefix if it's already set by the user
            return !nameAndVersion.Version.Contains('@')
                ? $"{nameAndVersion.Name}@{nameAndVersion.Version}"
                : nameAndVersion.Version;
        }
    }
}
