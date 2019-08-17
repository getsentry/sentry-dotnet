using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using NLog;
using NLog.Common;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;

using Sentry.Extensibility;
using Sentry.Infrastructure;
using Sentry.Protocol;
using Sentry.Reflection;

namespace Sentry.NLog
{
    /// <summary>
    /// Sentry NLog Target.
    /// </summary>
    [Target("Sentry")]
    public sealed class SentryTarget : TargetWithContext
    {
        private readonly Func<IHub> _hubAccessor;

        // For testing:
        internal Func<IHub> HubAccessor => _hubAccessor;

        private readonly ISystemClock _clock;
        private IDisposable _sdkDisposable;

        internal static readonly SdkVersion NameAndVersion = typeof(SentryTarget).Assembly.GetNameAndVersion();

        private static readonly string ProtocolPackageName = "nuget:" + NameAndVersion.Name;

        /// <summary>
        /// Creates a new instance of <see cref="SentryTarget"/>.
        /// </summary>
        public SentryTarget() : this(new SentryNLogOptions())
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="SentryTarget"/>.
        /// </summary>
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

        /// <summary>
        /// Options for both the <see cref="SentryTarget"/> and the sentry sdk itself.
        /// </summary>
        [Advanced]
        public SentryNLogOptions Options { get; }

        /// <summary>
        /// Add any desired additional tags that will be sent with every message.
        /// </summary>
        [ArrayParameter(typeof(TargetPropertyWithContext), "tag")]
        public IList<TargetPropertyWithContext> Tags => Options.Tags;

        /// <summary>
        /// The Data Source Name of a given project in Sentry.
        /// </summary>
        public string Dsn
        {
            get => Options.Dsn?.ToString();
            set => Options.Dsn = value == null ? null : new Dsn(value);
        }

        /// <summary>
        /// An optional layout specific to breadcrumbs. If not set, uses the same layout as the standard <see cref="TargetWithContext.Layout"/>.
        /// </summary>
        public Layout BreadcrumbLayout
        {
            get => Options.BreadcrumbLayout ?? Layout;
            set => Options.BreadcrumbLayout = value;
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

        /// <summary>
        /// Whether the NLog integration should initialize the SDK.
        /// </summary>
        /// <remarks>
        /// By default, if a DSN is provided to the NLog integration it will initialize the SDK.
        /// This might be not ideal when using multiple integrations in case you want another one doing the Init.
        /// </remarks>
        public bool InitializeSdk
        {
            get => Options.InitializeSdk;
            set => Options.InitializeSdk = value;
        }

        /// <summary>
        /// Set this to <see langword="true" /> to ignore log messages that don't contain an exception.
        /// </summary>
        public bool IgnoreEventsWithNoException
        {
            get => Options.IgnoreEventsWithNoException;
            set => Options.IgnoreEventsWithNoException = value;
        }

        /// <summary>
        /// Determines whether event-level properties will be sent to sentry as additional data.
        /// Defaults to <see langword="true" />.
        /// </summary>
        /// <seealso cref="SendEventPropertiesAsTags" />
        public bool SendEventPropertiesAsData
        {
            get => Options.SendEventPropertiesAsData;
            set => Options.SendEventPropertiesAsData = value;
        }

        /// <summary>
        /// Determines whether event properties will be sent to sentry as Tags or not.
        /// Defaults to <see langword="false" />.
        /// </summary>
        /// <seealso cref="SendEventPropertiesAsData"/>
        public bool SendEventPropertiesAsTags
        {
            get => Options.SendEventPropertiesAsTags;
            set => Options.SendEventPropertiesAsTags = value;
        }

        /// <summary>
        /// Determines whether or not to include event-level data as data in breadcrumbs for future errors.
        /// Defaults to <see langword="false" />.
        /// </summary>
        public bool IncludeEventDataOnBreadcrumbs
        {
            get => Options.IncludeEventDataOnBreadcrumbs;
            set => Options.IncludeEventDataOnBreadcrumbs = value;
        }

        /// <summary>
        /// How many seconds to wait after triggering <see cref="LogManager.Shutdown()"/> before just shutting down the
        /// Sentry sdk.
        /// </summary>
        public int ShutdownTimeoutSeconds
        {
            get => Options.ShutdownTimeoutSeconds;
            set => Options.ShutdownTimeoutSeconds = value;
        }

        /// <summary>
        /// How long to wait for the flush to finish, in seconds. Defaults to 2 seconds.
        /// </summary>
        public int FlushTimeoutSeconds
        {
            get => (int)Options.FlushTimeout.TotalSeconds;
            set => Options.FlushTimeout = TimeSpan.FromSeconds(value);
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

            // If a layout has been configured on the options, replace the default logger.
            if (Options.Layout != null)
            {
                Layout = Options.Layout;
            }

            // If the sdk is not there, set it on up.
            if (InitializeSdk && _sdkDisposable == null)
            {
                _sdkDisposable = SentrySdk.Init(Options);
            }
        }

        /// <inheritdoc />
        protected override void FlushAsync(AsyncContinuation asyncContinuation)
        {
            _hubAccessor()
                .FlushAsync(Options.FlushTimeout)
                .ContinueWith(t => asyncContinuation(t.Exception));
        }

        /// <summary>
        /// <para>
        /// If the event level &gt;= the <see cref="MinimumEventLevel"/>, the
        /// <paramref name="logEvent"/> is captured as an event by sentry.
        /// </para>
        /// <para>
        /// If the event level is &gt;= the <see cref="MinimumBreadcrumbLevel"/>, the event is added
        /// as a breadcrumb to the Sentry Sdk.
        /// </para>
        /// <para>
        /// If sentry is not enabled, this is a No-op.
        /// </para>
        /// </summary>
        /// <param name="logEvent">The event that is being logged.</param>
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

            var shouldOnlyLogExceptions = exception == null && IgnoreEventsWithNoException;

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
                    Logger = logEvent.LoggerName,
                    Level = logEvent.Level.ToSentryLevel(),
                    Release = Options.Release,
                    Environment = Options.Environment,
                };

                evt.Sdk.AddPackage(ProtocolPackageName, NameAndVersion.Version);

                // Always apply any manually configured tags
                evt.SetTags(GetDefaultTags(logEvent));

                if (IncludeEventProperties)
                {
                    evt.SetExtras(contextProps);
                }

                if (Options.SendEventPropertiesAsTags)
                {
                    evt.SetTags(GetTagsFromProperties(logEvent));
                }

                hub.CaptureEvent(evt);
            }

            // Whether or not it was sent as event, add breadcrumb so the next event includes it
            if (logEvent.Level >= Options.MinimumBreadcrumbLevel)
            {
                var breadcrumbFormatted = BreadcrumbLayout.Render(logEvent);

                var message = string.IsNullOrWhiteSpace(breadcrumbFormatted)
                    ? exception?.Message ?? string.Empty
                    : breadcrumbFormatted;

                IDictionary<string, string> data = null;

                // If this is true, an exception is being logged with no custom message
                if (exception != null && !message.StartsWith(exception.Message))
                {
                    // Exception won't be used as Breadcrumb message. Avoid losing it by adding as data:
                    data = new Dictionary<string, string>
                        {
                            { "exception_type", exception.GetType().ToString() },
                            { "exception_message", exception.Message },
                        };
                }

                if (IncludeEventDataOnBreadcrumbs)
                {
                    if (data is null)
                    {
                        data = new Dictionary<string, string>();
                    }

                    foreach (var contextProp in contextProps)
                    {
                        data.Add(contextProp.Key, contextProp.Value.ToString());
                    }
                }

                hub.AddBreadcrumb(
                    _clock,
                    message,
                    data: data,
                    level: logEvent.Level.ToBreadcrumbLevel());
            }
        }

        internal static IEnumerable<KeyValuePair<string, string>> GetTagsFromProperties(LogEventInfo logEvent)
        {
            if (!logEvent.HasProperties)
            {
                yield break;
            }

            foreach (var kv in logEvent.Properties)
            {
                yield return new KeyValuePair<string, string>(kv.Key.ToString(), kv.Value.ToString());
            }
        }

        private IEnumerable<KeyValuePair<string, string>> GetDefaultTags(LogEventInfo logEvent)
            => Tags.Select(tag =>
                new KeyValuePair<string, string>(tag.Name, tag.Layout.Render(logEvent)));
    }
}
