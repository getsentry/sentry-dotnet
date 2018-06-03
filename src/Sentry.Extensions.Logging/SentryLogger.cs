using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Sentry.Extensibility;
using Sentry.Infrastructure;

namespace Sentry.Extensions.Logging
{
    internal sealed class SentryLogger : ILogger
    {
        private readonly ISentryClient _sentryClient;
        private readonly ISystemClock _clock;
        private readonly SentryLoggingOptions _options;

        internal string CategoryName { get; }

        public SentryLogger(
            string categoryName,
            SentryLoggingOptions options)
            : this(
                categoryName,
                options,
                SystemClock.Clock,
                SentryCoreAdapter.Instance)
        {
        }

        internal SentryLogger(
            string categoryName,
            SentryLoggingOptions options,
            ISystemClock clock,
            ISentryClient sentryClient)
        {
            Debug.Assert(categoryName != null);
            Debug.Assert(options != null);
            Debug.Assert(clock != null);
            Debug.Assert(sentryClient != null);
            CategoryName = categoryName;
            _options = options;
            _clock = clock;
            _sentryClient = sentryClient;
        }

        public IDisposable BeginScope<TState>(TState state) => _sentryClient.PushScope(state);

        public bool IsEnabled(LogLevel logLevel) => _sentryClient.IsEnabled
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

            if (_options.MinimumEventLevel != LogLevel.None
                && logLevel >= _options.MinimumEventLevel)
            {
                var @event = new SentryEvent(exception)
                {
                    Logger = CategoryName,
                };

                if (message != null)
                {
                    // TODO: this will override the current message
                    // which could have been set from reading Exception.Message
                    if (@event.Message != null)
                    {
                        @event.SetExtra("original_message", @event.Message);
                    }
                    @event.Message = message;
                }

                var tuple = eventId.ToTupleOrNull();
                if (tuple.HasValue)
                {
                    @event.SetTag(tuple.Value.name, tuple.Value.value);
                }

                _sentryClient.CaptureEvent(@event);
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

                _sentryClient.AddBreadcrumb(
                        _clock,
                        message ?? exception?.Message,
                        "default",
                        CategoryName,
                        data,
                        logLevel.ToBreadcrumbLevel());
            }
        }
    }
}
