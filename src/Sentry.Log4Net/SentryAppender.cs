using System;
using log4net.Appender;
using log4net.Core;

namespace Sentry.Log4Net
{
    public class SentryAppender : AppenderSkeleton
    {
        private IDisposable _sdkHandle;
        private readonly object _initSync = new object();

        public string Dsn { get; set; }

        protected override void Append(LoggingEvent loggingEvent)
        {
            if (loggingEvent == null)
            {
                return;
            }

            if (Dsn != null && _sdkHandle == null)
            {
                lock (_initSync)
                {
                    if (_sdkHandle == null)
                    {
                        _sdkHandle = SentrySdk.Init(Dsn);
                    }
                }
            }

            // TODO: Scope
            //ThreadContext.Properties;

            var exception = loggingEvent.ExceptionObject ?? loggingEvent.MessageObject as Exception;
            var evt = new SentryEvent(exception);

            if (!string.IsNullOrWhiteSpace(loggingEvent.RenderedMessage))
            {
                evt.Message = loggingEvent.RenderedMessage;
            }

            evt.Logger = loggingEvent.LoggerName;
            evt.Level = loggingEvent.ToSentryLevel();

            SentrySdk.CaptureEvent(evt);
        }
        protected override void Append(LoggingEvent[] loggingEvents)
        {
            foreach (var loggingEvent in loggingEvents)
            {
                Append(loggingEvent);
            }
        }
        protected override void OnClose()
        {
            _sdkHandle?.Dispose();

            base.OnClose();
        }
    }
}
