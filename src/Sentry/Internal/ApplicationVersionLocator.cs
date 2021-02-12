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

            return $"{nameAndVersion.Name}@{nameAndVersion.Version}";
        }
    }
}
