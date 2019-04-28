using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using NLog;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;

using Sentry.Extensibility;
using Sentry.Infrastructure;
using Sentry.Protocol;
using Sentry.Reflection;

namespace Sentry.NLog
{
    [Target("Sentry")]
    public sealed class SentryTarget : TargetWithContext
    {
        private readonly Func<IHub> _hubAccessor;
        private readonly ISystemClock _clock;
        private IDisposable _sdkDisposable;

        internal static readonly (string Name, string Version) NameAndVersion = typeof(SentryTarget).Assembly.GetNameAndVersion();

        private static readonly string ProtocolPackageName = "nuget:" + NameAndVersion.Name;

        public SentryTarget() : this(new SentryNLogOptions())
        {
        }

        public SentryTarget(SentryNLogOptions options)
            : this(
                options,
                () => HubAdapter.Instance,
                sdkInstance: null,
                SystemClock.Clock)
        {
        }

        internal SentryTarget(SentryNLogOptions options, Func<IHub> hubAccessor, IDisposable sdkInstance, ISystemClock clock)
        {
            Debug.Assert(options != null);
            Debug.Assert(hubAccessor != null);
            Debug.Assert(clock != null);

            // Overrides default layout. Still will be explicitly overwritten if manually configured in the
            // NLog.config file.
            Layout = "${message}";

            Options = options;
            _hubAccessor = hubAccessor;
            _clock = clock;

            if (sdkInstance != null)
            {
                _sdkDisposable = sdkInstance;
            }
        }

        public SentryNLogOptions Options { get; }

        #region Xml property setters

        /// <summary>
        /// Add any desired additional tags that will be sent with every message.
        /// </summary>
        /// <inheritdoc />
        [ArrayParameter(typeof(TargetPropertyWithContext), "tag")]
        public override IList<TargetPropertyWithContext> ContextProperties { get; } = new List<TargetPropertyWithContext>();

        /// <summary>
        /// The Data Source Name of a given project in Sentry.
        /// </summary>
        [RequiredParameter]
        public string Dsn
        {
            get => Options.Dsn?.ToString();
            set => Options.Dsn = new Dsn(value);
        }

        /// <summary>
        /// Minimum log level for events to trigger a send to Sentry. Defaults to <see cref="LogLevel.Error" />.
        /// </summary>
        public string MinimumEventLevel
        {
            get => Options.MinimumEventLevel?.ToString() ?? LogLevel.Off.ToString();
            set => Options.MinimumEventLevel = LogLevel.FromString(value);
        }

        /// <summary>
        /// Minimum log level to be included in the breadcrumb. Defaults to <see cref="LogLevel.Info" />.
        /// </summary>
        public string MinimumBreadcrumbLevel
        {
            get => Options.MinimumBreadcrumbLevel?.ToString() ?? LogLevel.Off.ToString();
            set => Options.MinimumBreadcrumbLevel = LogLevel.FromString(value);
        }

        #endregion Xml property setters

        #region Lifecycle methods

        /// <inheritdoc />
        protected override void CloseTarget()
        {
            _sdkDisposable?.Dispose();
            base.CloseTarget();
        }

        /// <inheritdoc />
        protected override void InitializeTarget()
        {
            base.InitializeTarget();

            foreach (var prop in Options.Tags)
            {
                ContextProperties?.Add(new TargetPropertyWithContext(prop.Key, prop.Value));
            }

            // If the sdk is not there, set it on up.
            if (Options.InitializeSdk && _sdkDisposable == null)
            {
                _sdkDisposable = SentrySdk.Init(Options);
            }

        }

        /// <inheritdoc />
        protected override void Write(LogEventInfo logEvent)
        {
            if (logEvent?.Message == null)
            {
                return;
            }

            var hub = _hubAccessor();

            if (hub?.IsEnabled != true)
            {
                return;
            }

            var exception = logEvent.Exception;
            var formatted = Layout.Render(logEvent);
            var template = logEvent.Message;

            var shouldOnlyLogExceptions = logEvent.Exception == null && Options.IgnoreEventsWithNoException;

            if (logEvent.Level >= Options.MinimumEventLevel && !shouldOnlyLogExceptions)
            {
                var evt = new SentryEvent(exception)
                {
                    Sdk = {
                        Name = Constants.SdkName,
                        Version = NameAndVersion.Version
                    },
                    Message = null,
                    LogEntry = new LogEntry
                    {
                        Formatted = formatted,
                        Message = template
                    },
                    Level = logEvent.Level.ToSentryLevel(),
                    Release = Options.Release,
                    Environment = Options.Environment,
                };

                evt.Sdk.AddPackage(ProtocolPackageName, NameAndVersion.Version);

                if (Options.SendContextPropertiesAsTags)
                {
                    evt.SetTags(GetLoggingContextProperties(logEvent));
                }

                var eventProps = GetLoggingEventProperties(logEvent).ToList();

                if (Options.SendLogEventInfoPropertiesAsTags)
                {
                    evt.SetTags(eventProps.Select(a => new KeyValuePair<string, string>(a.Key, a.Value.ToString())));
                }

                if (Options.SendLogEventInfoPropertiesAsData)
                {
                    evt.SetExtras(eventProps);
                }

                hub.CaptureEvent(evt);
            }

            // Whether or not it was sent as event, add breadcrumb so next event includes it
            if (logEvent.Level >= Options.MinimumBreadcrumbLevel)
            {
                IDictionary<string, string> data = null;

                // If this is true, an exception is being logged with no custom message
                if (exception != null && !string.IsNullOrWhiteSpace(formatted) && logEvent.Message != "{0}")
                {
                    // Exception.Message won't be used as Breadcrumb message Avoid losing it by adding as data:
                    data = new Dictionary<string, string>
                            {
                                {"exception_message", exception.Message}
                            };
                }

                var message = string.IsNullOrWhiteSpace(formatted)
                    ? exception?.Message
                    : formatted;

                hub.AddBreadcrumb(
                    _clock,
                    message,
                    data: data,
                    level: logEvent.Level.ToBreadcrumbLevel());
            }

        }

        #endregion Lifecycle methods

        private IEnumerable<KeyValuePair<string, string>> GetLoggingContextProperties(LogEventInfo logEvent)
        {
            var props = ContextProperties.ToKeyValuePairs(a => a.Name, a => a.Layout?.Render(logEvent));

            foreach (var item in props.DistinctBy(a => a.Key))
            {
                yield return item;
            }

        }

        private IEnumerable<KeyValuePair<string, object>> GetLoggingEventProperties(LogEventInfo logEvent)
        {
            var eventProperties = new Dictionary<string, object>();

            if (logEvent.HasProperties)
            {
                eventProperties = logEvent.Properties.ToDictionary(x => x.Key.ToString(), x => x.Value);
            }

            return eventProperties;
        }

    }
}
