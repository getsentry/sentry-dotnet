using System;

namespace Sentry.Tests.Helpers
{
    internal static class EnvironmentVariableGuard
    {
        public static void WithVariable(string key, string value, Action action)
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
