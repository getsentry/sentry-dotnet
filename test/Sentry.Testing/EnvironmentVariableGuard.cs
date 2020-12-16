using System;

namespace Sentry.Testing
{
    public static class EnvironmentVariableGuard
    {
        // To allow different xunit collections use of this
        private static readonly object Lock = new();

        public static void WithVariable(string key, string value, Action action)
        {
            lock (Lock)
            {
                Environment.SetEnvironmentVariable(key, value, EnvironmentVariableTarget.Process);
                try
                {
                    action();
                }
                finally
                {
                    Environment.SetEnvironmentVariable(key, null, EnvironmentVariableTarget.Process);
                }
            }
        }
    }
}
