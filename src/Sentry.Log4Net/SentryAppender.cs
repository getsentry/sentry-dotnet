using System;
using System.Collections.Generic;
using log4net.Appender;
using log4net.Core;
using Sentry.Extensibility;
using Sentry.Protocol;
using Sentry.Reflection;

namespace Sentry.Log4Net
{
    /// <summary>
    /// Sentry appender for log4net.
    /// </summary>
    public class SentryAppender : AppenderSkeleton
    {
        private readonly Func<string, IDisposable> _initAction;
        private volatile IDisposable? _sdkHandle;

        private readonly object _initSync = new();

        internal static readonly SdkVersion NameAndVersion
            = typeof(SentryAppender).Assembly.GetNameAndVersion();

        private static readonly string ProtocolPackageName = "nuget:" + NameAndVersion.Name;

        internal IHub Hub { get; set; }

        /// <summary>
        /// Sentry DSN.
        /// </summary>
        public string? Dsn { get; set; }
        /// <summary>
        /// Whether to send the Identity or not.
        /// </summary>
        public bool SendIdentity { get; set; }
        /// <summary>
        /// Environment to send in the event.
        /// </summary>
        public string? Environment { get; set; }

        /// <summary>
        /// Creates a new instance of the <see cref="SentryAppender"/>.
        /// </summary>
        public SentryAppender() : this(SentrySdk.Init, HubAdapter.Instance)
        { }

        internal SentryAppender(
            Func<string, IDisposable> initAction,
            IHub hubGetter)
        {
            _initAction = initAction;
            Hub = hubGetter;
        }

        /// <summary>
        /// Append log.
        /// </summary>
        /// <param name="loggingEvent">The event.</param>
        protected override void Append(LoggingEvent loggingEvent)
        {
            // Not to throw on code that ignores nullability warnings.
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (loggingEvent is null)
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
                    // ReSharper disable once NonAtomicCompoundOperator Double init guarded by the lock
                    _sdkHandle ??= _initAction(Dsn);
                }
            }

            var exception = loggingEvent.ExceptionObject ?? loggingEvent.MessageObject as Exception;
            var evt = new SentryEvent(exception)
            {
                Logger = loggingEvent.LoggerName,
                Level = loggingEvent.ToSentryLevel()
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

            if (!string.IsNullOrWhiteSpace(Environment))
            {
                evt.Environment = Environment;
            }

            _ = Hub.CaptureEvent(evt);
        }

        private static IEnumerable<KeyValuePair<string, object?>> GetLoggingEventProperties(LoggingEvent loggingEvent)
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
                        yield return new KeyValuePair<string, object?>(key, value);
                    }
                }
            }

            var locInfo = loggingEvent.LocationInformation;
            if (locInfo != null)
            {
                if (!string.IsNullOrEmpty(locInfo.ClassName))
                {
                    yield return new KeyValuePair<string, object?>(nameof(locInfo.ClassName), locInfo.ClassName);
                }

                if (!string.IsNullOrEmpty(locInfo.FileName))
                {
                    yield return new KeyValuePair<string, object?>(nameof(locInfo.FileName), locInfo.FileName);
                }

                if (int.TryParse(locInfo.LineNumber, out var lineNumber) && lineNumber != 0)
                {
                    yield return new KeyValuePair<string, object?>(nameof(locInfo.LineNumber), lineNumber);
                }

                if (!string.IsNullOrEmpty(locInfo.MethodName))
                {
                    yield return new KeyValuePair<string, object?>(nameof(locInfo.MethodName), locInfo.MethodName);
                }
            }

            if (!string.IsNullOrEmpty(loggingEvent.ThreadName))
            {
                yield return new KeyValuePair<string, object?>(nameof(loggingEvent.ThreadName), loggingEvent.ThreadName);
            }

            if (!string.IsNullOrEmpty(loggingEvent.Domain))
            {
                yield return new KeyValuePair<string, object?>(nameof(loggingEvent.Domain), loggingEvent.Domain);
            }

            if (loggingEvent.Level != null)
            {
                yield return new KeyValuePair<string, object?>("log4net-level", loggingEvent.Level.Name);
            }
        }

        /// <summary>
        /// Disposes the SDK if initialized.
        /// </summary>
        protected override void OnClose()
        {
            base.OnClose();

            _sdkHandle?.Dispose();
        }
    }
}
