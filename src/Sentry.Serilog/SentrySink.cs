using System;
using System.Collections.Generic;
using System.Diagnostics;
using Sentry.Extensibility;
using Sentry.Infrastructure;
using Sentry.Protocol;
using Sentry.Reflection;
using Serilog.Core;
using Serilog.Events;

namespace Sentry.Serilog
{
    /// <summary>
    /// Sentry Sink for Serilog
    /// </summary>
    /// <inheritdoc cref="IDisposable" />
    /// <inheritdoc cref="ILogEventSink" />
    internal sealed class SentrySink : ILogEventSink, IDisposable
    {
        private readonly IDisposable _sdkDisposable;
        private readonly SentrySerilogOptions _options;

        internal static readonly (string Name, string Version) NameAndVersion
            = typeof(SentrySink).Assembly.GetNameAndVersion();

        private static readonly string ProtocolPackageName = "nuget:" + NameAndVersion.Name;

        private readonly Func<IHub> _hubAccessor;
        private readonly ISystemClock _clock;

        public SentrySink(
            SentrySerilogOptions options,
            IDisposable sdkDisposable)
            : this(
                options,
                () => HubAdapter.Instance,
                sdkDisposable,
                SystemClock.Clock)
        {
        }

        internal SentrySink(
            SentrySerilogOptions options,
            Func<IHub> hubAccessor,
            IDisposable sdkDisposable,
            ISystemClock clock)
        {
            Debug.Assert(options != null);
            Debug.Assert(hubAccessor != null);
            Debug.Assert(clock != null);

            _options = options;
            _hubAccessor = hubAccessor;
            _clock = clock;
            _sdkDisposable = sdkDisposable;
        }

        public void Emit(LogEvent logEvent)
        {
            if (logEvent == null)
            {
                return;
            }

            var hub = _hubAccessor();
            if (hub == null || !hub.IsEnabled)
            {
                return;
            }

            var exception = logEvent.Exception;
            var formatted = logEvent.RenderMessage(_options.FormatProvider);
            var template = logEvent.MessageTemplate.Text;

            if (logEvent.Level >= _options.MinimumEventLevel)
            {
                var evt = new SentryEvent(exception)
                {
                    Sdk =
                    {
                        Name = Constants.SdkName,
                        Version = NameAndVersion.Version
                    },
                    Message = null,
                    LogEntry = new LogEntry
                    {
                        Formatted = formatted,
                        Message = template
                    },
                    Level = logEvent.Level.ToSentryLevel()
                };

                evt.Sdk.AddPackage(ProtocolPackageName, NameAndVersion.Version);
                evt.SetExtras(GetLoggingEventProperties(logEvent));

                hub.CaptureEvent(evt);
            }

            // Even if it was sent as event, add breadcrumb so next event includes it
            if (logEvent.Level >= _options.MinimumBreadcrumbLevel)
            {
                Dictionary<string, string> data = null;
                if (exception != null && !string.IsNullOrWhiteSpace(formatted))
                {
                    // Exception.Message won't be used as Breadcrumb message
                    // Avoid losing it by adding as data:
                    data = new Dictionary<string, string>
                    {
                        {"exception_message", exception.Message}
                    };
                }

                hub.AddBreadcrumb(
                    _clock,
                    message: string.IsNullOrWhiteSpace(formatted)
                        ? exception?.Message
                        : formatted,
                    data: data,
                    level: logEvent.Level.ToBreadcrumbLevel());
            }
        }

        private IEnumerable<KeyValuePair<string, object>> GetLoggingEventProperties(LogEvent logEvent)
        {
            var properties = logEvent.Properties;

            foreach (var property in properties)
            {
                var value = property.Value;
                if (value is ScalarValue scalarValue)
                {
                    yield return new KeyValuePair<string, object>(property.Key, scalarValue.Value);
                }
                else if (value != null)
                {
                    yield return new KeyValuePair<string, object>(property.Key, value);
                }
            }
        }

        public void Dispose() => _sdkDisposable?.Dispose();
    }
}
