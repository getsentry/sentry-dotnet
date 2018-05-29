using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Sentry.Extensibility;
using Sentry.Infrastructure;
using Sentry.Protocol;

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

        public IDisposable BeginScope<TState>(TState state)
        {
            var guard = _sdk.PushScope(state);

            // TODO: store state within Scope to be read later when (if) event is sent

            return guard;
        }

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
                    Message = message,
                };

                var tuple = eventId.ToTupleOrNull();
                if (tuple.HasValue)
                {
                    @event.AddTag(tuple.Value.name, tuple.Value.value);
                }

                _sdk.CaptureEvent(@event);
            }
            else if (_options.MinimumBreadcrumbLevel != LogLevel.None
                     && logLevel >= _options.MinimumBreadcrumbLevel)
            {
                _sdk.ConfigureScope(
                    s => s.AddBreadcrumb(
                        _clock,
                        message,
                        "logger",
                        CategoryName,
                        eventId.ToTupleOrNull(),
                        logLevel.ToBreadcrumbLevel()));
            }
        }
    }
}
