#if NET461
using System;
using Microsoft.Win32;

namespace Sentry.PlatformAbstractions
{
    internal static class RegistryKeyExtensions
    {
        public static string? GetString(this RegistryKey key, string value)
            => key.GetValue(value) as string;

        public static int? GetInt(this RegistryKey key, string value)
            => (int?)key.GetValue(value);

        private static Exception? _lastException { get; set; }

        internal static Exception? GetLastException(this RegistryKey _)
            => _lastException;

        internal static RegistryKey? TryOpenLocalSubKey(this RegistryKey key, string path)
        {
            try
            {
                return _lastException != null ? null : key.OpenSubKey(path, false);
            }
            catch (Exception ex)
            {
                _lastException = ex;
                return null;
            }
        }
    }
}
#endif
