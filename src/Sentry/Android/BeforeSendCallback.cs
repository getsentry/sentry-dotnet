using System;
using Sentry.Extensibility;
using JavaObject = Java.Lang.Object;

namespace Sentry.Android
{
    internal class BeforeSendCallback : JavaObject, Java.SentryOptions.IBeforeSendCallback
    {
        private readonly Func<SentryEvent, SentryEvent?> _beforeSend;
        private readonly IDiagnosticLogger? _logger;
        private readonly Java.SentryOptions _javaOptions;

        public BeforeSendCallback(
            Func<SentryEvent, SentryEvent?> beforeSend,
            IDiagnosticLogger? logger,
            Java.SentryOptions javaOptions)
        {
            _beforeSend = beforeSend;
            _logger = logger;
            _javaOptions = javaOptions;
        }

        public Java.SentryEvent? Execute(Java.SentryEvent e, Java.Hint h)
        {
            // Note: Hint is unused due to:
            // https://github.com/getsentry/sentry-dotnet/issues/1469

            var evnt = e.ToSentryEvent(_javaOptions);
            var result = _beforeSend.Invoke(evnt);
            return result?.ToJavaSentryEvent(_logger, _javaOptions);
        }
    }
}
