using System.Reflection;
using Sentry.Reflection;

namespace Sentry.Internal
{
    internal static class ApplicationVersionLocator
    {
        private static volatile bool _read;
        private static string _version;

        public static string GetCurrent()
        {
            if (_read)
            {
                return _version;
            }

            try
            {
                if (_read)
                {
                    return _version;
                }

                var version = Assembly.GetEntryAssembly()?.GetNameAndVersion().Version;
                if (version != string.Empty
                    // If it really was 1.0, it would need to be set explicitly since this is the default.
                    && version != "1.0.0"
                    && version != "1.0.0.0")
                {
                    _version = version;
                }

                return _version;
            }
            finally
            {
                _read = true;
            }
        }
    }
}
