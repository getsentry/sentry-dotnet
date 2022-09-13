using Sentry.Android.Extensions;

namespace Sentry.Android;

internal class JavaLogger : JavaObject, Java.ILogger
{
    private readonly SentryOptions _options;

    public JavaLogger(SentryOptions options) => _options = options;

    public void Log(Java.SentryLevel level, string message, JavaObject[]? args) =>
        _options.DiagnosticLogger?.Log(level.ToSentryLevel(), "Android: " + FormatJavaString(message, args));

    public void Log(Java.SentryLevel level, string message, Throwable? throwable) =>
        _options.DiagnosticLogger?.Log(level.ToSentryLevel(), "Android: " + message, throwable);

    public void Log(Java.SentryLevel level, Throwable? throwable, string message, params JavaObject[]? args) =>
        _options.DiagnosticLogger?.Log(level.ToSentryLevel(), "Android: " + FormatJavaString(message, args), throwable);

    public bool IsEnabled(Java.SentryLevel? level) =>
        level != null && _options.DiagnosticLogger?.IsEnabled(level.ToSentryLevel()) == true;

    private static string FormatJavaString(string s, JavaObject[]? args) => args is null ? s : JavaString.Format(s, args);
}
