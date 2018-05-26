using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Sentry.Infrastructure;
using Sentry.Protocol;

namespace Sentry.Extensions.Logging
{
    internal sealed class SentryLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly ISystemClock _clock;
        private readonly SentryLoggingOptions _options;

        public SentryLogger(
            string categoryName,
            SentryLoggingOptions options,
            ISystemClock clock = null)
        {
            Debug.Assert(categoryName != null);
            Debug.Assert(options != null);
            _categoryName = categoryName;
            _options = options;
            _clock = clock ?? SystemClock.Clock;
        }

        public IDisposable BeginScope<TState>(TState state) => SentryCore.PushScope();

        public bool IsEnabled(LogLevel logLevel) => SentryCore.IsEnabled && logLevel >= _options.MinimumBreadcrumbLevel;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception exception,
            Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var message = formatter?.Invoke(state, exception);

            // If it's enabled, at least Breadcrumb has to be stored
            if (logLevel < _options.MinimumEventLevel)
            {
                SentryCore.ConfigureScope(s => s.AddBreadcrumb(FromLogEvent()));
            }
            else
            {
                var @event = new SentryEvent(exception)
                {
                    Logger = _categoryName,
                    Message = message
                };

                if (eventId.Id != 0 || eventId.Name != null)
                {
                    @event.AddTag(nameof(eventId), eventId.ToString());
                }

                SentryCore.CaptureEvent(@event);
            }

            Breadcrumb FromLogEvent()
            {
                var data = ImmutableDictionary<string, string>.Empty;
                if (eventId.Id != 0 || eventId.Name != null)
                {
                    data = ImmutableDictionary<string, string>.Empty
                        .Add(nameof(eventId), eventId.ToString());
                }

                return new Breadcrumb(
                    timestamp: _clock.GetUtcNow(),
                    message: message,
                    type: "logger",
                    data: data,
                    category: _categoryName,
                    level: logLevel.ToBreadcrumbLevel());
            }
        }
    }
}
