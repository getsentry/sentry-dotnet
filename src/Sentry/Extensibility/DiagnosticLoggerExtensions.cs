using System;

namespace Sentry.Extensibility
{
    // The generic overloads avoid boxing in case logging is disabled for that level
    internal static class DiagnosticLoggerExtensions
    {
        public static void LogDebug<TArg>(
            this IDiagnosticLogger logger,
            string message,
            TArg arg)
            => logger.LogIfEnabled(SentryLevel.Debug, message, arg);

        public static void LogDebug<TArg, TArg2>(
            this IDiagnosticLogger logger,
            string message,
            TArg arg,
            TArg2 arg2)
            => logger.LogIfEnabled(SentryLevel.Debug, message, arg, arg2);

        public static void LogDebug(
            this IDiagnosticLogger logger,
            string message)
            => logger.LogIfEnabled(SentryLevel.Debug, message);

        public static void LogInfo(
            this IDiagnosticLogger logger,
            string message)
            => logger.LogIfEnabled(SentryLevel.Info, message);

        public static void LogInfo<TArg>(
            this IDiagnosticLogger logger,
            string message,
            TArg arg)
            => logger.LogIfEnabled(SentryLevel.Info, message, arg);

        public static void LogInfo<TArg, TArg2>(
            this IDiagnosticLogger logger,
            string message,
            TArg arg,
            TArg2 arg2)
            => logger.LogIfEnabled(SentryLevel.Info, message, arg, arg2);

        public static void LogWarning(
            this IDiagnosticLogger logger,
            string message)
            => logger.LogIfEnabled(SentryLevel.Warning, message);

        public static void LogWarning<TArg>(
            this IDiagnosticLogger logger,
            string message,
            TArg arg)
            => logger.LogIfEnabled(SentryLevel.Warning, message, arg);

        public static void LogWarning<TArg, TArg2>(
            this IDiagnosticLogger logger,
            string message,
            TArg arg,
            TArg2 arg2)
            => logger.LogIfEnabled(SentryLevel.Warning, message, arg, arg2);

        public static void LogError(
            this IDiagnosticLogger logger,
            string message,
            Exception? exception = null)
            => logger.LogIfEnabled(SentryLevel.Error, message, exception);

        public static void LogError<TArg>(
            this IDiagnosticLogger logger,
            string message,
            Exception exception,
            TArg arg)
            => logger.LogIfEnabled(SentryLevel.Error, message, exception, arg);

        public static void LogError<TArg, TArg2>(
            this IDiagnosticLogger logger,
            string message,
            Exception exception,
            TArg arg,
            TArg2 arg2)
            => logger.LogIfEnabled(SentryLevel.Error, message, exception, arg, arg2);

        public static void LogFatal(
            this IDiagnosticLogger logger,
            string message,
            Exception? exception = null)
            => logger.LogIfEnabled(SentryLevel.Fatal, message, exception);

        internal static void LogIfEnabled(
            this IDiagnosticLogger logger,
            SentryLevel level,
            string message,
            Exception? exception = null)
        {
            if (logger.IsEnabled(level))
            {
                logger.Log(level, message, exception);
            }
        }

        internal static void LogIfEnabled<TArg>(
            this IDiagnosticLogger logger,
            SentryLevel level,
            string message,
            TArg arg,
            Exception? exception = null)
        {
            if (logger.IsEnabled(level))
            {
                logger.Log(level, message, exception, arg);
            }
        }

        internal static void LogIfEnabled<TArg, TArg2>(
            this IDiagnosticLogger logger,
            SentryLevel level,
            string message,
            TArg arg,
            TArg2 arg2,
            Exception? exception = null)
        {
            if (logger.IsEnabled(level))
            {
                logger.Log(level, message, exception, arg, arg2);
            }
        }

        internal static void LogIfEnabled<TArg, TArg2, TArg3>(
            this IDiagnosticLogger logger,
            SentryLevel level,
            string message,
            TArg arg,
            TArg2 arg2,
            TArg3 arg3,
            Exception? exception = null)
        {
            if (logger.IsEnabled(level))
            {
                logger.Log(level, message, exception, arg, arg2, arg3);
            }
        }
    }
}
