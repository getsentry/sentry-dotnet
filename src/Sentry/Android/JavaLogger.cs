using System.Linq;
using Java.Lang;
using JavaObject = Java.Lang.Object;

namespace Sentry.Android
{
    internal class JavaLogger : JavaObject, Java.ILogger
    {
        private readonly SentryOptions _options;

        public JavaLogger(SentryOptions options) => _options = options;

        public void Log(Java.SentryLevel level, string message, JavaObject[]? args) =>
            _options.DiagnosticLogger?.Log(level.ToSentryLevel(), message, null, args?.Cast<object?>());

        public void Log(Java.SentryLevel level, string message, Throwable? throwable) =>
            _options.DiagnosticLogger?.Log(level.ToSentryLevel(), message, throwable);

        public void Log(Java.SentryLevel level, Throwable? throwable, string message, params JavaObject[]? args) =>
            _options.DiagnosticLogger?.Log(level.ToSentryLevel(), message, throwable, args?.Cast<object?>());

        public bool IsEnabled(Java.SentryLevel? level) =>
            level != null && _options.DiagnosticLogger?.IsEnabled(level.ToSentryLevel()) == true;
    }
}
