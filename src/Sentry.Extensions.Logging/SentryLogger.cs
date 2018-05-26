using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Sentry.Infrastructure;
using Sentry.Protocol;

namespace Sentry.Extensions.Logging
{
    internal sealed class SentryLogger : ILogger
    {
        private readonly ISystemClock _clock;
        private readonly SentryLoggingOptions _options;

        internal string CategoryName { get; }

        public SentryLogger(
            string categoryName,
            SentryLoggingOptions options,
            ISystemClock clock = null)
        {
            Debug.Assert(categoryName != null);
            Debug.Assert(options != null);
            CategoryName = categoryName;
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

            // If it's enabled, level is configured to at least store event as Breadcrumb
            if (logLevel < _options.MinimumEventLevel)
            {
                SentryCore.ConfigureScope(
                    s => s.AddBreadcrumb(
                        message,
                        "logger",
                        CategoryName,
                        eventId.ToTupleOrNull(),
                        logLevel.ToBreadcrumbLevel()));
            }
            else
            {
                var @event = new SentryEvent(exception)
                {
                    Logger = CategoryName,
                    Message = message,
                };

                var tuple = eventId.ToTupleOrNull();
                if (tuple.HasValue)
                {
                    @event.AddTag(tuple.Value.name, tuple.Value.value);
                }

                SentryCore.CaptureEvent(@event);
            }
        }
    }
}
