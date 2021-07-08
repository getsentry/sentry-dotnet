using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Extensions.Logging;
using Sentry.Infrastructure;
using Sentry.Protocol;

namespace Sentry.Extensions.Logging
{
    internal sealed class SentryLogger : ILogger
    {
        private class DelayedSpan : Span
        {

            public new DateTimeOffset StartTimestamp { get; set; }

            public DelayedSpan(ISpan tracer, DateTimeOffset startTimestamp)
                : base(tracer.ParentSpanId, tracer.Operation)
            {
                StartTimestamp = startTimestamp;
            }

        }

        private readonly IHub _hub;
        private readonly ISystemClock _clock;
        private readonly SentryLoggingOptions _options;
        private AsyncLocal<ISpan?> _span = new AsyncLocal<ISpan?>();

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
            if (!IsEnabled(logLevel) || ShouldFinishSpan(eventId))
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


            if (ShouldStartSpan(eventId))
            {
                var span = SentrySdk.GetSpan()?.StartChild("Logger db", "bob");
                _span.Value?.Finish();
                _span.Value = span;
            }
            else if (ShouldFinishSpan(eventId))
            {
                // Executed || DbCommand || TIME || SQL...
                var data = message?.Split(new char[] { ' ' }, 4);
                if (data?.Length == 4 && Regex.Match(data[2], @"\d+") is { } match &&
                        match.Success)
                {
                    var oldTimeStamp = DateTime.UtcNow.AddMilliseconds(-double.Parse(match.Value));
                    if (SentrySdk.GetSpan() is { } span &&
                        span.StartChild("db", "logger") is SpanTracer spanTracer)
                    {
                        spanTracer.StartTimestamp = oldTimeStamp;
                        spanTracer.Finish();
                    }
                }
            }
            // Even if it was sent as event, add breadcrumb so next event includes it
            else if (ShouldAddBreadcrumb(logLevel, eventId, exception))
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

        private bool ShouldStartSpan(EventId eventId)
            => eventId.ToString() == "Microsoft.EntityFrameworkCore.Database.Command.CommandExecuting";

        private bool ShouldFinishSpan(EventId eventid)
            => eventid.ToString() == "Microsoft.EntityFrameworkCore.Database.Command.CommandExecuted";
    }
}
