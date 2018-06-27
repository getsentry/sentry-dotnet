using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Logging;
using Sentry.Infrastructure;

namespace Sentry.Extensions.Logging
{
    internal sealed class SentryLogger : ILogger
    {
        private readonly IHub _hub;
        private readonly ISystemClock _clock;
        private readonly SentryLoggingOptions _options;

        internal string CategoryName { get; }

        internal SentryLogger(
            string categoryName,
            SentryLoggingOptions options,
            ISystemClock clock,
            IHub hub)
        {
            Debug.Assert(categoryName != null);
            Debug.Assert(options != null);
            Debug.Assert(clock != null);
            Debug.Assert(hub != null);
            CategoryName = categoryName;
            _options = options;
            _clock = clock;
            _hub = hub;
        }

        public IDisposable BeginScope<TState>(TState state) => _hub.PushScope(state);

        public bool IsEnabled(LogLevel logLevel)
            => _hub.IsEnabled
                && logLevel != LogLevel.None
                && (logLevel >= _options.MinimumBreadcrumbLevel
                || logLevel >= _options.MinimumEventLevel);

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

            if (SendEvent(logLevel, eventId, exception))
            {
                var @event = new SentryEvent(exception)
                {
                    Logger = CategoryName,
                    Message = message,
                };

                var tuple = eventId.ToTupleOrNull();
                if (tuple.HasValue)
                {
                    @event.SetTag(tuple.Value.name, tuple.Value.value);
                }

                _hub.CaptureEvent(@event);
            }

            // Even if it was sent as event, add breadcrumb so next event includes it
            if (_options.MinimumBreadcrumbLevel != LogLevel.None
                     && logLevel >= _options.MinimumBreadcrumbLevel)
            {
                var data = eventId.ToDictionaryOrNull();
                if (exception != null && message != null)
                {
                    // Exception.Message won't be used as Breadcrumb message
                    // Avoid losing it by adding as data:
                    data = data ?? new Dictionary<string, string>();
                    data.Add("exception_message", exception.Message);
                }

                _hub.AddBreadcrumb(
                    _clock,
                    message ?? exception?.Message,
                    CategoryName,
                    // TODO: verify on sentry
                    type: null,
                    data,
                    logLevel.ToBreadcrumbLevel());
            }
        }

        private bool SendEvent(
            LogLevel logLevel,
            EventId eventId,
            Exception exception)
                => _options.MinimumEventLevel != LogLevel.None
                   && logLevel >= _options.MinimumEventLevel
                   // No events from Sentry code using ILogger
                   && !CategoryName.StartsWith("Sentry")
                   && (_options.Filters == null
                        || _options.Filters.All(
                           f => !f.Filter(
                               CategoryName,
                               logLevel,
                               eventId,
                               exception)));
    }
}
