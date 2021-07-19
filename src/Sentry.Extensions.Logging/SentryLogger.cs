using System;
using System.Collections.Generic;
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
            Exception? exception,
            Func<TState, Exception?, string>? formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var message = formatter?.Invoke(state, exception);

            if (ShouldCaptureEvent(logLevel, eventId, exception))
            {
                var @event = new SentryEvent(exception)
                {
                    Logger = CategoryName,
                    Message = message,
                    Level = logLevel.ToSentryLevel()
                };

                if (state is IEnumerable<KeyValuePair<string, object>> pairs)
                {
                    foreach (var property in pairs)
                    {
                        if (property.Key == "{OriginalFormat}" && property.Value is string template)
                        {
                            // Original format found, use Sentry logEntry interface
                            @event.Message = new SentryMessage
                            {
                                Formatted = message,
                                Message = template
                            };
                            continue;
                        }

                        if (property.Value is string tagValue)
                        {
                            @event.SetTag(property.Key, tagValue);
                        }
                    }
                }

                var tuple = eventId.ToTupleOrNull();
                if (tuple.HasValue)
                {
                    @event.SetTag(tuple.Value.name, tuple.Value.value);
                }

                _ = _hub.CaptureEvent(@event);
            }

            // Even if it was sent as event, add breadcrumb so next event includes it
            if (ShouldAddBreadcrumb(logLevel, eventId, exception))
            {
                var data = eventId.ToDictionaryOrNull();

                if (exception != null && message != null)
                {
                    // Exception.Message won't be used as Breadcrumb message
                    // Avoid losing it by adding as data:
                    data ??= new Dictionary<string, string>();
                    data.Add("exception_message", exception.Message);
                }

                _hub.AddBreadcrumb(
                    _clock,
                    message ?? exception?.Message!,
                    CategoryName,
                    null,
                    data,
                    logLevel.ToBreadcrumbLevel());
            }
        }

        private bool ShouldCaptureEvent(
            LogLevel logLevel,
            EventId eventId,
            Exception? exception)
                => _options.MinimumEventLevel != LogLevel.None
                   && logLevel >= _options.MinimumEventLevel
                   // No events from Sentry code using ILogger
                   // A type from the main SDK could be used to resolve a logger
                   // hence 'Sentry' and also 'Sentry.', won't block SentrySomething
                   // often used by users experimenting with Sentry
                   && !CategoryName.StartsWith("Sentry.", StringComparison.Ordinal)
                   && !string.Equals(CategoryName, "Sentry", StringComparison.Ordinal)
                   && _options.Filters.All(
                       f => !f.Filter(
                           CategoryName,
                           logLevel,
                           eventId,
                           exception));

        private bool ShouldAddBreadcrumb(
            LogLevel logLevel,
            EventId eventId,
            Exception? exception)
            => _options.MinimumBreadcrumbLevel != LogLevel.None
               && logLevel >= _options.MinimumBreadcrumbLevel
               && _options.Filters.All(
                   f => !f.Filter(
                       CategoryName,
                       logLevel,
                       eventId,
                       exception))
               && !CategoryName.StartsWith("Sentry.", StringComparison.Ordinal)
               && !string.Equals(CategoryName, "Sentry", StringComparison.Ordinal);
    }
}
