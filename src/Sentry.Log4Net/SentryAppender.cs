using System;
using System.Collections.Generic;
using System.Diagnostics;
using log4net.Appender;
using log4net.Core;
using Sentry.Extensibility;
using Sentry.Protocol;
using Sentry.Reflection;

namespace Sentry.Log4Net
{
    public class SentryAppender : AppenderSkeleton
    {
        private readonly Func<string, IDisposable> _initAction;
        private volatile IDisposable _sdkHandle;

        private readonly object _initSync = new object();

        internal static readonly (string Name, string Version) NameAndVersion
            = typeof(SentryAppender).Assembly.GetNameAndVersion();

        private static readonly string ProtocolPackageName = "nuget:" + NameAndVersion.Name;

        internal IHub Hub { get; set; }

        public string Dsn { get; set; }
        public bool SendIdentity { get; set; }

        public SentryAppender() : this(SentrySdk.Init, HubAdapter.Instance)
        { }

        internal SentryAppender(
            Func<string, IDisposable> initAction,
            IHub hubGetter)
        {
            Debug.Assert(initAction != null);
            Debug.Assert(hubGetter != null);

            _initAction = initAction;
            Hub = hubGetter;
        }

        protected override void Append(LoggingEvent loggingEvent)
        {
            if (loggingEvent == null)
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

            var exception = loggingEvent.ExceptionObject ?? loggingEvent.MessageObject as Exception;
            var evt = new SentryEvent(exception)
            {
                Sdk =
                {
                    Name = Constants.SdkName,
                    Version = NameAndVersion.Version
                },
                Logger = loggingEvent.LoggerName,
                Level = loggingEvent.ToSentryLevel()
            };

            evt.Sdk.AddPackage(ProtocolPackageName, NameAndVersion.Version);

            if (!string.IsNullOrWhiteSpace(loggingEvent.RenderedMessage))
            {
                evt.Message = loggingEvent.RenderedMessage;
            }

            evt.SetExtras(GetLoggingEventProperties(loggingEvent));

            if (SendIdentity && !string.IsNullOrEmpty(loggingEvent.Identity))
            {
                evt.User = new User
                {
                    Id = loggingEvent.Identity
                };
            }

            Hub.CaptureEvent(evt);
        }

        private static IEnumerable<KeyValuePair<string, object>> GetLoggingEventProperties(LoggingEvent loggingEvent)
        {
            var properties = loggingEvent.GetProperties();
            if (properties == null)
            {
                yield break;
            }

            foreach (var key in properties.GetKeys())
            {
                if (!string.IsNullOrWhiteSpace(key)
                    && !key.StartsWith("log4net:", StringComparison.OrdinalIgnoreCase))
                {
                    var value = properties[key];
                    if (value != null
                        && (!(value is string stringValue) || !string.IsNullOrWhiteSpace(stringValue)))
                    {
                        yield return new KeyValuePair<string, object>(key, value);
                    }
                }
            }

            var locInfo = loggingEvent.LocationInformation;
            if (locInfo != null)
            {
                if (!string.IsNullOrEmpty(locInfo.ClassName))
                {
                    yield return new KeyValuePair<string, object>(nameof(locInfo.ClassName), locInfo.ClassName);
                }

                if (!string.IsNullOrEmpty(locInfo.FileName))
                {
                    yield return new KeyValuePair<string, object>(nameof(locInfo.FileName), locInfo.FileName);
                }

                if (int.TryParse(locInfo.LineNumber, out var lineNumber) && lineNumber != 0)
                {
                    yield return new KeyValuePair<string, object>(nameof(locInfo.LineNumber), lineNumber);
                }

                if (!string.IsNullOrEmpty(locInfo.MethodName))
                {
                    yield return new KeyValuePair<string, object>(nameof(locInfo.MethodName), locInfo.MethodName);
                }
            }

            if (!string.IsNullOrEmpty(loggingEvent.ThreadName))
            {
                yield return new KeyValuePair<string, object>(nameof(loggingEvent.ThreadName), loggingEvent.ThreadName);
            }

            if (!string.IsNullOrEmpty(loggingEvent.Domain))
            {
                yield return new KeyValuePair<string, object>(nameof(loggingEvent.Domain), loggingEvent.Domain);
            }

            if (loggingEvent.Level != null)
            {
                yield return new KeyValuePair<string, object>("log4net-level", loggingEvent.Level.Name);
            }
        }

        protected override void OnClose()
        {
            base.OnClose();

            _sdkHandle?.Dispose();
        }
    }
}
