using System;
using System.Collections.Generic;
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
        private readonly IDisposable? _sdkDisposable;
        private readonly SentrySerilogOptions _options;

        internal static readonly SdkVersion NameAndVersion
            = typeof(SentrySink).Assembly.GetNameAndVersion();

        private static readonly string ProtocolPackageName = "nuget:" + NameAndVersion.Name;

        private readonly Func<IHub> _hubAccessor;
        private readonly ISystemClock _clock;

        public SentrySink(
            SentrySerilogOptions options,
            IDisposable? sdkDisposable)
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
            IDisposable? sdkDisposable,
            ISystemClock clock)
        {
            _options = options;
            _hubAccessor = hubAccessor;
            _clock = clock;
            _sdkDisposable = sdkDisposable;
        }

        public void Emit(LogEvent logEvent)
        {
            string? context = null;

            if (logEvent.Properties.TryGetValue("SourceContext", out var prop)
                && prop is ScalarValue scalar
                && scalar.Value is string sourceContextValue)
            {
                if (sourceContextValue.StartsWith("Sentry.")
                    || string.Equals(sourceContextValue, "Sentry", StringComparison.Ordinal))
                {
                    return;
                }

                context = sourceContextValue;
            }

            var hub = _hubAccessor();
            if (hub is null || !hub.IsEnabled)
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
                    Logger = context,
                    Message = new SentryMessage
                    {
                        Formatted = formatted,
                        Message = template
                    },
                    Level = logEvent.Level.ToSentryLevel()
                };

                if (evt.Sdk is {} sdk)
                {
                    sdk.Name = Constants.SdkName;
                    sdk.Version = NameAndVersion.Version;

                    if (NameAndVersion.Version is {} version)
                    {
                        sdk.AddPackage(ProtocolPackageName, version);
                    }
                }

                evt.SetExtras(GetLoggingEventProperties(logEvent));

                _ = hub.CaptureEvent(evt);
            }

            // Even if it was sent as event, add breadcrumb so next event includes it
            if (logEvent.Level >= _options.MinimumBreadcrumbLevel)
            {
                Dictionary<string, string> ?data = null;
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
                    string.IsNullOrWhiteSpace(formatted)
                        ? exception?.Message ?? ""
                        : formatted,
                    context,
                    data: data,
                    level: logEvent.Level.ToBreadcrumbLevel());
            }
        }

        private IEnumerable<KeyValuePair<string, object?>> GetLoggingEventProperties(LogEvent logEvent)
        {
            var properties = logEvent.Properties;

            foreach (var property in properties)
            {
                var value = property.Value;
                if (value is ScalarValue scalarValue)
                {
                    yield return new KeyValuePair<string, object?>(property.Key, scalarValue.Value);
                }
                else if (value != null)
                {
                    yield return new KeyValuePair<string, object?>(property.Key, value);
                }
            }
        }

        public void Dispose() => _sdkDisposable?.Dispose();
    }
}
