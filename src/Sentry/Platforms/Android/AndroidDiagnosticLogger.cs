using Sentry.Android.Extensions;
using Sentry.Extensibility;

namespace Sentry.Android;

internal class AndroidDiagnosticLogger : JavaObject, JavaSdk.ILogger
{
    private readonly IDiagnosticLogger? _logger;

    public AndroidDiagnosticLogger(IDiagnosticLogger? logger) => _logger = logger;

    // Without these, net10.0-android invokes the Java default methods non-virtually and throws AbstractMethodError
    public void Log(JavaSdk.SentryLevel level, string message) =>
        Log(level, message, Array.Empty<JavaObject>());

    public void Log(JavaSdk.SentryLevel level, string message, JavaObject? arg) =>
        Log(level, message, [arg!]);

    public void Log(JavaSdk.SentryLevel level, string message, JavaObject? arg1, JavaObject? arg2) =>
        Log(level, message, [arg1!, arg2!]);

    public void Log(JavaSdk.SentryLevel level, string message, JavaObject[]? args) =>
        _logger?.Log(level.ToSentryLevel(), "Android: " + FormatJavaString(message, args));

    public void Log(JavaSdk.SentryLevel level, string message, Throwable? throwable) =>
        _logger?.Log(level.ToSentryLevel(), "Android: " + message, throwable);

    public void Log(JavaSdk.SentryLevel level, Throwable? throwable, string message, params JavaObject[]? args) =>
        _logger?.Log(level.ToSentryLevel(), "Android: " + FormatJavaString(message, args), throwable);

    public bool IsEnabled(JavaSdk.SentryLevel? level) =>
        level != null && _logger != null && _logger.IsEnabled(level.ToSentryLevel());

    private static string FormatJavaString(string s, JavaObject[]? args) =>
        args is null ? s : JavaString.Format(s, args);
}
