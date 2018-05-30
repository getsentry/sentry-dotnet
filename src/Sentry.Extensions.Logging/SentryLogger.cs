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
        private readonly ISdk _sdk;
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
            ISdk sdk)
        {
            Debug.Assert(categoryName != null);
            Debug.Assert(options != null);
            Debug.Assert(clock != null);
            Debug.Assert(sdk != null);
            CategoryName = categoryName;
            _options = options;
            _clock = clock;
            _sdk = sdk;
        }

        public IDisposable BeginScope<TState>(TState state) => _sdk.PushScope(state);

        public bool IsEnabled(LogLevel logLevel) => _sdk.IsEnabled
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
                        @event.AddTag("message", @event.Message);
                    }
                    @event.Message = message;
                }

                var tuple = eventId.ToTupleOrNull();
                if (tuple.HasValue)
                {
                    @event.AddTag(tuple.Value.name, tuple.Value.value);
                }

                _sdk.CaptureEvent(@event);
            }

            // Even if it was sent as event, add breadcrumb so next event includes it
            if (_options.MinimumBreadcrumbLevel != LogLevel.None
                     && logLevel >= _options.MinimumBreadcrumbLevel)
            {

                var data = eventId.ToDictionaryOrNull();
                if (exception != null)
                {
                    data = data ?? new Dictionary<string, string>();
                    data.Add("exception.message", exception.Message);
                    data.Add("exception.stacktrace", exception.StackTrace);
                }

                _sdk.AddBreadcrumb(
                        _clock,
                        message ?? exception?.Message,
                        "logger",
                        CategoryName,
                        data,
                        logLevel.ToBreadcrumbLevel());
            }
        }
    }
}
