using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using NLog;
using NLog.Config;
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

        /// <summary>
        /// Add any desired additional tags that will be sent with every message.
        /// </summary>
        [ArrayParameter(typeof(TargetPropertyWithContext), "tag")]
        public IList<TargetPropertyWithContext> Tags => Options.Tags;

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
        /// Minimum log level for events to trigger a send to Sentry. Defaults to <see cref="M:LogLevel.Error" />.
        /// </summary>
        public string MinimumEventLevel
        {
            get => Options.MinimumEventLevel?.ToString() ?? LogLevel.Off.ToString();
            set => Options.MinimumEventLevel = LogLevel.FromString(value);
        }

        /// <summary>
        /// Minimum log level to be included in the breadcrumb. Defaults to <see cref="M:LogLevel.Info" />.
        /// </summary>
        public string MinimumBreadcrumbLevel
        {
            get => Options.MinimumBreadcrumbLevel?.ToString() ?? LogLevel.Off.ToString();
            set => Options.MinimumBreadcrumbLevel = LogLevel.FromString(value);
        }

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

            IncludeEventProperties = Options.SendEventPropertiesAsData;

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

            var contextProps = GetAllProperties(logEvent);

            var shouldOnlyLogExceptions = exception == null && Options.IgnoreEventsWithNoException;

            if (logEvent.Level >= Options.MinimumEventLevel && !shouldOnlyLogExceptions)
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
                    Level = logEvent.Level.ToSentryLevel(),
                    Release = Options.Release,
                    Environment = Options.Environment,
                };

                evt.Sdk.AddPackage(ProtocolPackageName, NameAndVersion.Version);

                // Always apply any manually configured tags
                evt.SetTags(GetTags(logEvent));

                if (IncludeEventProperties)
                {
                    evt.SetExtras(contextProps);
                }

                if (Options.SendEventPropertiesAsTags)
                {
                    evt.SetTags(GetLoggingEventProperties(logEvent).MapValues(a => a.ToString()));
                }

                hub.CaptureEvent(evt);
            }

            // Whether or not it was sent as event, add breadcrumb so the next event includes it
            if (logEvent.Level >= Options.MinimumBreadcrumbLevel)
            {
                var message = string.IsNullOrWhiteSpace(formatted)
                    ? exception?.Message ?? string.Empty
                    : formatted;

                IDictionary<string, string> data = null;

                // If this is true, an exception is being logged with no custom message
                if (exception != null && !message.StartsWith(exception.Message))
                {
                    // Exception.Message won't be used as Breadcrumb message Avoid losing it by adding as data:
                    data = new Dictionary<string, string>
                        {
                            { "exception_type", exception.GetType().ToString() },
                            { "exception_message", exception.Message },
                        };
                }

                if (Options.IncludeEventDataOnBreadcrumbs)
                {
                    if (data is null)
                    {
                        data = new Dictionary<string, string>();
                    }

                    data.AddRange(contextProps.MapValues(a => a.ToString()));
                }

                hub.AddBreadcrumb(
                    _clock,
                    message,
                    data: data,
                    level: logEvent.Level.ToBreadcrumbLevel());
            }
        }

        private IEnumerable<KeyValuePair<string, string>> GetTags(LogEventInfo logEvent)
        {
            return Tags.ToKeyValuePairs(a => a.Name, a => a.Layout.Render(logEvent));
        }

        private IEnumerable<KeyValuePair<string, object>> GetLoggingEventProperties(LogEventInfo logEvent)
        {
            if (!logEvent.HasProperties)
            {
                return Enumerable.Empty<KeyValuePair<string, object>>();
            }

            return logEvent.Properties.ToKeyValuePairs(x => x.Key.ToString(), x => x.Value);
        }

    }
}
