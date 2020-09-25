#if NETFX
using Microsoft.Win32;

namespace Sentry.PlatformAbstractions
{
    internal static class RegistryKeyExtensions
    {
        public static string? GetString(this RegistryKey key, string value)
            => key.GetValue(value) as string;

        public static int? GetInt(this RegistryKey key, string value)
            => (int?)key.GetValue(value);
    }
}
#endif
