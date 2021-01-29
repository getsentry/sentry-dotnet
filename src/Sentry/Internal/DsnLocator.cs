using System;
using System.Reflection;

namespace Sentry.Internal
{
    internal static class DsnLocator
    {
        /// <summary>
        /// Attempts to find a DSN string statically (via env var, asm attribute). Returns Disabled token otherwise.
        /// </summary>
        internal static string FindDsnStringOrDisable(Assembly? asm = null)
            => Environment.GetEnvironmentVariable(Constants.DsnEnvironmentVariable)
               ?? FindDsn(asm)
               ?? Sentry.Constants.DisableSdkDsnValue;

        /// <summary>
        /// Attempts to find a DSN string from the entry assembly's DsnAttribute.
        /// </summary>
        /// <returns>DSN string or null if none found.</returns>
        internal static string? FindDsn(Assembly? asm = null)
            => (asm ?? Assembly.GetEntryAssembly())?.GetCustomAttribute<DsnAttribute>()?.Dsn;
    }
}
