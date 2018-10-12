using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Sentry.Extensibility;
using Sentry.Protocol;
using Sentry.Reflection;
using Serilog.Core;
using Serilog.Events;

namespace Sentry.Serilog
{
    public sealed class SentrySink : ILogEventSink, IDisposable
    {
        private readonly IFormatProvider _formatProvider;
        private readonly Func<string, IDisposable> _initAction;
        private volatile IDisposable _sdkHandle;

        private readonly object _initSync = new object();

        internal static readonly (string Name, string Version) NameAndVersion
            = typeof(SentrySink).Assembly.GetNameAndVersion();

        private static readonly string ProtocolPackageName = "nuget:" + NameAndVersion.Name;

        internal IHub Hub { get; set; }

        public string Dsn { get; set; }

        public SentrySink(IFormatProvider formatProvider) : this(formatProvider, SentrySdk.Init, HubAdapter.Instance)
        { }

        internal SentrySink(
            IFormatProvider formatProvider,
            Func<string, IDisposable> initAction,
            IHub hubGetter)
        {
            Debug.Assert(initAction != null);
            Debug.Assert(hubGetter != null);

            _formatProvider = formatProvider;
            _initAction = initAction;
            Hub = hubGetter;
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
                Message = logEvent.RenderMessage(_formatProvider),
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

        public void Dispose()
        {
            _sdkHandle?.Dispose();
        }
    }
}
