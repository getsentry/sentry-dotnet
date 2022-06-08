namespace Sentry.Android
{
    internal class JavaLogger : JavaObject, Java.ILogger
    {
        private readonly SentryOptions _options;

        public JavaLogger(SentryOptions options) => _options = options;

        public void Log(Java.SentryLevel level, string message, JavaObject[]? args) =>
            _options.DiagnosticLogger?.Log((SentryLevel)level, message, null, args?.Cast<object?>());

        public void Log(Java.SentryLevel level, string message, Throwable? throwable) =>
            _options.DiagnosticLogger?.Log((SentryLevel)level, message, throwable);

        public void Log(Java.SentryLevel level, Throwable? throwable, string message, params JavaObject[]? args) =>
            _options.DiagnosticLogger?.Log((SentryLevel)level, message, throwable, args?.Cast<object?>());

        public bool IsEnabled(Java.SentryLevel level) =>
            _options.DiagnosticLogger?.IsEnabled((SentryLevel)level) == true;
    }
}
