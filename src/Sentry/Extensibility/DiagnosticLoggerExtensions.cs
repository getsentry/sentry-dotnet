using System;
using Sentry.Protocol;

namespace Sentry.Extensibility
{
    internal static class DiagnosticLoggerExtensions
    {
        public static void LogDebug(
            this IDiagnosticLogger logger,
            string message,
            params object[] args)
            => logger.LogIfEnabled(SentryLevel.Debug, message, args: args);

        public static void LogInfo(
            this IDiagnosticLogger logger,
            string message,
            params object[] args)
            => logger.LogIfEnabled(SentryLevel.Info, message, args: args);

        public static void LogWarning(
            this IDiagnosticLogger logger,
            string message,
            Exception exception = null,
            params object[] args)
            => logger.LogIfEnabled(SentryLevel.Warning, message, exception, args);

        public static void LogError(
            this IDiagnosticLogger logger,
            string message,
            Exception exception = null,
            params object[] args)
            => logger.LogIfEnabled(SentryLevel.Error, message, exception, args);

        public static void LogFatal(
            this IDiagnosticLogger logger,
            string message,
            Exception exception = null,
            params object[] args)
            => logger.LogIfEnabled(SentryLevel.Fatal, message, exception, args);

        private static void LogIfEnabled(
            this IDiagnosticLogger logger,
            SentryLevel level,
            string message,
            Exception exception = null,
            params object[] args)
        {
            if (logger.IsEnabled(level))
            {
                logger.Log(level, message, exception, args);
            }
        }
    }
}
