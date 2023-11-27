namespace Sentry.Extensibility;

/// <summary>
/// The generic overloads avoid boxing in case logging is disabled for that level
/// </summary>
/// <remarks>
/// Calls to this class verify the level before calling the overload with object params.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class DiagnosticLoggerExtensions
{
    /// <summary>
    /// Log an internal SDK message.
    /// </summary>
    internal static void Log(
        this SentryOptions options,
        SentryLevel logLevel,
        string message,
        Exception? exception = null,
        params object?[] args)
        => options.DiagnosticLogger?.Log(logLevel, message, exception, args);

    /// <summary>
    /// Log a debug message.
    /// </summary>
    public static void LogDebug<TArg>(
        this IDiagnosticLogger logger,
        string message,
        TArg arg)
        => logger.LogIfEnabled(SentryLevel.Debug, null, message, arg);

    /// <summary>
    /// Log a debug message.
    /// </summary>
    internal static void LogDebug<TArg>(
        this SentryOptions options,
        string message,
        TArg arg)
        => options.DiagnosticLogger?.LogIfEnabled(SentryLevel.Debug, null, message, arg);

    /// <summary>
    /// Log a debug message.
    /// </summary>
    public static void LogDebug<TArg, TArg2>(
        this IDiagnosticLogger logger,
        string message,
        TArg arg,
        TArg2 arg2)
        => logger.LogIfEnabled(SentryLevel.Debug, null, message, arg, arg2);

    /// <summary>
    /// Log a debug message.
    /// </summary>
    internal static void LogDebug<TArg, TArg2>(
        this SentryOptions options,
        string message,
        TArg arg,
        TArg2 arg2)
        => options.DiagnosticLogger?.LogIfEnabled(SentryLevel.Debug, null, message, arg, arg2);

    /// <summary>
    /// Log a debug message.
    /// </summary>
    public static void LogDebug(
        this IDiagnosticLogger logger,
        string message)
        => logger.LogIfEnabled(SentryLevel.Debug, null, message);

    /// <summary>
    /// Log a debug message.
    /// </summary>
    internal static void LogDebug(
        this SentryOptions options,
        string message)
        => options.DiagnosticLogger?.LogIfEnabled(SentryLevel.Debug, null, message);

    /// <summary>
    /// Log a info message.
    /// </summary>
    public static void LogInfo(
        this IDiagnosticLogger logger,
        string message)
        => logger.LogIfEnabled(SentryLevel.Info, null, message);

    /// <summary>
    /// Log a info message.
    /// </summary>
    internal static void LogInfo(
        this SentryOptions options,
        string message)
        => options.DiagnosticLogger?.LogIfEnabled(SentryLevel.Info, null, message);

    /// <summary>
    /// Log a info message.
    /// </summary>
    public static void LogInfo<TArg>(
        this IDiagnosticLogger logger,
        string message,
        TArg arg)
        => logger.LogIfEnabled(SentryLevel.Info, null, message, arg);

    /// <summary>
    /// Log a info message.
    /// </summary>
    internal static void LogInfo<TArg>(
        this SentryOptions options,
        string message,
        TArg arg)
        => options.DiagnosticLogger?.LogIfEnabled(SentryLevel.Info, null, message, arg);

    /// <summary>
    /// Log a info message.
    /// </summary>
    public static void LogInfo<TArg, TArg2>(
        this IDiagnosticLogger logger,
        string message,
        TArg arg,
        TArg2 arg2)
        => logger.LogIfEnabled(SentryLevel.Info, null, message, arg, arg2);

    /// <summary>
    /// Log a info message.
    /// </summary>
    internal static void LogInfo<TArg, TArg2>(
        this SentryOptions options,
        string message,
        TArg arg,
        TArg2 arg2)
        => options.DiagnosticLogger?.LogIfEnabled(SentryLevel.Info, null, message, arg, arg2);

    /// <summary>
    /// Log a info message.
    /// </summary>
    public static void LogInfo<TArg, TArg2, TArg3>(
        this IDiagnosticLogger logger,
        string message,
        TArg arg,
        TArg2 arg2,
        TArg3 arg3)
        => logger.LogIfEnabled(SentryLevel.Info, null, message, arg, arg2, arg3);

    /// <summary>
    /// Log a info message.
    /// </summary>
    internal static void LogInfo<TArg, TArg2, TArg3>(
        this SentryOptions options,
        string message,
        TArg arg,
        TArg2 arg2,
        TArg3 arg3)
        => options.DiagnosticLogger?.LogIfEnabled(SentryLevel.Info, null, message, arg, arg2, arg3);

    /// <summary>
    /// Log a warning message.
    /// </summary>
    public static void LogWarning(
        this IDiagnosticLogger logger,
        string message,
        Exception? exception = null)
        => logger.LogIfEnabled(SentryLevel.Warning, exception, message);

    /// <summary>
    /// Log a warning message.
    /// </summary>
    internal static void LogWarning(
        this SentryOptions options,
        string message,
        Exception? exception = null)
        => options.DiagnosticLogger?.LogIfEnabled(SentryLevel.Warning, exception, message);

    /// <summary>
    /// Log a warning message.
    /// </summary>
    public static void LogWarning<TArg>(
        this IDiagnosticLogger logger,
        string message,
        TArg arg)
        => logger.LogIfEnabled(SentryLevel.Warning, null, message, arg);

    /// <summary>
    /// Log a warning message.
    /// </summary>
    internal static void LogWarning<TArg>(
        this SentryOptions options,
        string message,
        TArg arg)
        => options.DiagnosticLogger?.LogIfEnabled(SentryLevel.Warning, null, message, arg);

    /// <summary>
    /// Log a warning message.
    /// </summary>
    public static void LogWarning<TArg, TArg2>(
        this IDiagnosticLogger logger,
        string message,
        TArg arg,
        TArg2 arg2)
        => logger.LogIfEnabled(SentryLevel.Warning, null, message, arg, arg2);

    /// <summary>
    /// Log a warning message.
    /// </summary>
    internal static void LogWarning<TArg, TArg2>(
        this SentryOptions options,
        string message,
        TArg arg,
        TArg2 arg2)
        => options.DiagnosticLogger?.LogIfEnabled(SentryLevel.Warning, null, message, arg, arg2);

    /// <summary>
    /// Log a error message.
    /// </summary>
    public static void LogError(
        this IDiagnosticLogger logger,
        string message)
        => logger.LogIfEnabled(SentryLevel.Error, null, message);

    /// <summary>
    /// Log an exception with an error message.
    /// </summary>
    public static void LogError(this IDiagnosticLogger logger,
        Exception exception,
        string message)
        => logger.LogIfEnabled(SentryLevel.Error, exception, message);

    /// <summary>
    /// Log a error message.
    /// </summary>
    internal static void LogError(
        this SentryOptions options,
        string message)
        => options.DiagnosticLogger?.LogIfEnabled(SentryLevel.Error, null, message);

    /// <summary>
    /// Log a error message.
    /// </summary>
    internal static void LogError(this SentryOptions options,
        Exception exception,
        string message)
        => options.DiagnosticLogger?.LogIfEnabled(SentryLevel.Error, exception, message);

    /// <summary>
    /// Log a error message.
    /// </summary>
    public static void LogError<TArg>(this IDiagnosticLogger logger,
        Exception exception,
        string message,
        TArg arg)
        => logger.LogIfEnabled(SentryLevel.Error, exception, message, arg);

    /// <summary>
    /// Log a error message.
    /// </summary>
    internal static void LogError<TArg>(this SentryOptions options,
        Exception exception,
        string message,
        TArg arg)
        => options.DiagnosticLogger?.LogIfEnabled(SentryLevel.Error, exception, message, arg);

    /// <summary>
    /// Log a error message.
    /// </summary>
    public static void LogError<TArg, TArg2>(this IDiagnosticLogger logger,
        Exception exception,
        string message,
        TArg arg,
        TArg2 arg2)
        => logger.LogIfEnabled(SentryLevel.Error, exception, message, arg, arg2);

    /// <summary>
    /// Log a error message.
    /// </summary>
    internal static void LogError<TArg, TArg2>(this SentryOptions options,
        Exception exception,
        string message,
        TArg arg,
        TArg2 arg2)
        => options.DiagnosticLogger?.LogIfEnabled(SentryLevel.Error, exception, message, arg, arg2);

    /// <summary>
    /// Log a error message.
    /// </summary>
    public static void LogError<TArg, TArg2, TArg3, TArg4>(this IDiagnosticLogger logger,
        Exception exception,
        string message,
        TArg arg,
        TArg2 arg2,
        TArg3 arg3,
        TArg4 arg4)
        => logger.LogIfEnabled(SentryLevel.Error, exception, message, arg, arg2, arg3, arg4);

    /// <summary>
    /// Log a error message.
    /// </summary>
    internal static void LogError<TArg, TArg2, TArg3, TArg4>(this SentryOptions options,
        Exception exception,
        string message,
        TArg arg,
        TArg2 arg2,
        TArg3 arg3,
        TArg4 arg4)
        => options.DiagnosticLogger?.LogIfEnabled(SentryLevel.Error, exception, message, arg, arg2, arg3, arg4);

    /// <summary>
    /// Log an error message.
    /// </summary>
    public static void LogError<TArg, TArg2, TArg3>(
        this IDiagnosticLogger logger,
        Exception exception,
        string message,
        TArg arg,
        TArg2 arg2,
        TArg3 arg3)
        => logger.LogIfEnabled(SentryLevel.Error, exception, message, arg, arg2, arg3);

    /// <summary>
    /// Log an error message.
    /// </summary>
    internal static void LogError<TArg, TArg2, TArg3>(
        this SentryOptions options,
        Exception exception,
        string message,
        TArg arg,
        TArg2 arg2,
        TArg3 arg3)
        => options.DiagnosticLogger?.LogIfEnabled(SentryLevel.Error, exception, message, arg, arg2, arg3);

    /// <summary>
    /// Log a warning message.
    /// </summary>
    public static void LogFatal(
        this IDiagnosticLogger logger,
        string message)
        => logger.LogIfEnabled(SentryLevel.Fatal, null, message);

    /// <summary>
    /// Log an exception with a warning message.
    /// </summary>
    public static void LogFatal(this IDiagnosticLogger logger,
        Exception exception,
        string message)
        => logger.LogIfEnabled(SentryLevel.Fatal, exception, message);

    /// <summary>
    /// Log a warning message.
    /// </summary>
    internal static void LogFatal(
        this SentryOptions options,
        string message)
        => options.DiagnosticLogger?.LogIfEnabled(SentryLevel.Fatal, null, message);

    /// <summary>
    /// Log an exception with a warning message.
    /// </summary>
    internal static void LogFatal(this SentryOptions options,
        Exception exception,
        string message)
        => options.DiagnosticLogger?.LogIfEnabled(SentryLevel.Fatal, exception, message);

    internal static void LogIfEnabled(
        this IDiagnosticLogger logger,
        SentryLevel level,
        Exception? exception,
        string message)
    {
        if (logger.IsEnabled(level))
        {
            logger.Log(level, message, exception);
        }
    }

    internal static void LogIfEnabled(
        this SentryOptions options,
        SentryLevel level,
        Exception? exception,
        string message)
    {
        var logger = options.DiagnosticLogger;
        if (logger == null)
        {
            return;
        }

        if (logger.IsEnabled(level))
        {
            logger.Log(level, message, exception);
        }
    }

    internal static void LogIfEnabled<TArg>(
        this IDiagnosticLogger logger,
        SentryLevel level,
        Exception? exception,
        string message,
        TArg arg)
    {
        if (logger.IsEnabled(level))
        {
            logger.Log(level, message, exception, arg);
        }
    }

    internal static void LogIfEnabled<TArg>(
        this SentryOptions options,
        SentryLevel level,
        Exception? exception,
        string message,
        TArg arg)
    {
        var logger = options.DiagnosticLogger;
        if (logger == null)
        {
            return;
        }

        if (logger.IsEnabled(level))
        {
            logger.Log(level, message, exception, arg);
        }
    }

    internal static void LogIfEnabled<TArg, TArg2>(
        this IDiagnosticLogger logger,
        SentryLevel level,
        Exception? exception,
        string message,
        TArg arg,
        TArg2 arg2)
    {
        if (logger.IsEnabled(level))
        {
            logger.Log(level, message, exception, arg, arg2);
        }
    }

    internal static void LogIfEnabled<TArg, TArg2>(
        this SentryOptions options,
        SentryLevel level,
        Exception? exception,
        string message,
        TArg arg,
        TArg2 arg2)
    {
        var logger = options.DiagnosticLogger;
        if (logger == null)
        {
            return;
        }

        if (logger.IsEnabled(level))
        {
            logger.Log(level, message, exception, arg, arg2);
        }
    }

    internal static void LogIfEnabled<TArg, TArg2, TArg3>(
        this IDiagnosticLogger logger,
        SentryLevel level,
        Exception? exception,
        string message,
        TArg arg,
        TArg2 arg2,
        TArg3 arg3)
    {
        if (logger.IsEnabled(level))
        {
            logger.Log(level, message, exception, arg, arg2, arg3);
        }
    }

    internal static void LogIfEnabled<TArg, TArg2, TArg3>(
        this SentryOptions options,
        SentryLevel level,
        Exception? exception,
        string message,
        TArg arg,
        TArg2 arg2,
        TArg3 arg3)
    {
        var logger = options.DiagnosticLogger;
        if (logger == null)
        {
            return;
        }

        if (logger.IsEnabled(level))
        {
            logger.Log(level, message, exception, arg, arg2, arg3);
        }
    }

    internal static void LogIfEnabled<TArg, TArg2, TArg3, TArg4>(
        this IDiagnosticLogger logger,
        SentryLevel level,
        Exception? exception,
        string message,
        TArg arg,
        TArg2 arg2,
        TArg3 arg3,
        TArg4 arg4)
    {
        if (logger.IsEnabled(level))
        {
            logger.Log(level, message, exception, arg, arg2, arg3, arg4);
        }
    }

    internal static void LogIfEnabled<TArg, TArg2, TArg3, TArg4>(
        this SentryOptions options,
        SentryLevel level,
        Exception? exception,
        string message,
        TArg arg,
        TArg2 arg2,
        TArg3 arg3,
        TArg4 arg4)
    {
        var logger = options.DiagnosticLogger;
        if (logger == null)
        {
            return;
        }

        if (logger.IsEnabled(level))
        {
            logger.Log(level, message, exception, arg, arg2, arg3, arg4);
        }
    }
}
