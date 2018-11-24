using System;
using System.Collections.Generic;
using System.Diagnostics;
using Sentry.Extensibility;
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
    public sealed class SentrySink : ILogEventSink, IDisposable
    {
        private SentrySerilogOptions _options;

        private readonly object _initSync = new object();

        internal static readonly (string Name, string Version) NameAndVersion
            = typeof(SentrySink).Assembly.GetNameAndVersion();

        private static readonly string ProtocolPackageName = "nuget:" + NameAndVersion.Name;

        private IHub _hub;

        /// <summary>
        /// Creates a new instance of <see cref="SentrySink"/>.
        /// </summary>
        /// <param name="options">The Sentry Serilog options to configure the sink.</param>
        public SentrySink(SentrySerilogOptions options)
            : this(options, HubAdapter.Instance)
        { }

        internal SentrySink(
            SentrySerilogOptions options,
            IHub hub)
        {
            Debug.Assert(options != null);
            Debug.Assert(hub != null);

            Hub = hub;
        }

        public void Emit(LogEvent logEvent)
        {
            if (logEvent == null)
            {
                return;
            }

            if (!Hub.IsEnabled && _sdkHandle == null)
            {
                if (Dsn == null)
                {
                    return;
                }

                lock (_initSync)
                {
                    if (_sdkHandle == null)
                    {
                        _sdkHandle = _initAction(Dsn);
                        Debug.Assert(_sdkHandle != null);
                    }
                }
            }

            var exception = logEvent.Exception;

            var evt = new SentryEvent(exception)
            {
                Sdk =
                {
                    Name = Constants.SdkName,
                    Version = NameAndVersion.Version
                },
                LogEntry = new LogEntry
                {
                    Formatted = logEvent.RenderMessage(_options.FormatProvider),
                    Message = logEvent.MessageTemplate.Text
                },
                Level = logEvent.Level.ToSentryLevel()
            };

            evt.Sdk.AddPackage(ProtocolPackageName, NameAndVersion.Version);

            evt.SetExtras(GetLoggingEventProperties(logEvent));

            Hub.CaptureEvent(evt);
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

        public void Dispose() => _sdkHandle?.Dispose();
    }
}
