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
    /// <summary>
    /// Sentry Options for Serilog logging
    /// </summary>
    /// <inheritdoc />
    [NLogConfigurationItem]
    public class SentryNLogOptions : SentryOptions
    {
        public string DefaultEnvironment { get; set; }
        public LogLevel MinLogLevelForEvent { get; set; }
        public LogLevel MinimumBreadcrumbLevel { get; set; }
        public bool IgnoreEventsWithNoException { get; set; }
        public bool SendLogEventInfoPropertiesAsTags { get; set; }

    }

    [Target("sentry")]
    public class SentryTarget : TargetWithContext, IDisposable
    {
        private readonly Func<IHub> _hubAccessor;
        private readonly ISystemClock _clock;
        private readonly IDisposable _sdkDisposable;
        internal static readonly (string Name, string Version) NameAndVersion = typeof(SentryTarget).Assembly.GetNameAndVersion();

        private static readonly string ProtocolPackageName = "nuget:" + NameAndVersion.Name;

        public SentryTarget() : this(new SentryNLogOptions())
        {

        }

        private SentryTarget(SentryNLogOptions options) : this(options, SentrySdk.Init(options))
        {

        }

        public SentryTarget(
            SentryNLogOptions options,
            IDisposable sdkDisposable)
            : this(
                options,
                () => HubAdapter.Instance,
                sdkDisposable,
                SystemClock.Clock)
        {
        }

        internal SentryTarget(
            SentryNLogOptions options,
            Func<IHub> hubAccessor,
            IDisposable sdkDisposable,
            ISystemClock clock)
        {
            Debug.Assert(options != null);
            Debug.Assert(hubAccessor != null);
            Debug.Assert(clock != null);

            Options = options;
            _hubAccessor = hubAccessor;
            _clock = clock;
            _sdkDisposable = sdkDisposable;
        }

        [RequiredParameter]
        public string Dsn
        {
            get => Options.Dsn.SentryUri.ToString();
            set => Options.Dsn = new Dsn(value);
        }

        public SentryNLogOptions Options { get; }

        [ArrayParameter(typeof(TargetPropertyWithContext), "context")]
        public override IList<TargetPropertyWithContext> ContextProperties { get; } = new List<TargetPropertyWithContext>();

        /// <summary>
        /// Writes logging event to the log target.
        /// </summary>
        /// <param name="logEvent">Logging event to be written out.</param>
        protected override void Write(LogEventInfo logEvent)
        {
            if (logEvent == null)
            {
                return;
            }

            var hub = _hubAccessor();
            if (hub?.IsEnabled != true)
            {
                return;
            }


            if (logEvent.Exception == null && Options.IgnoreEventsWithNoException)
            {
                return;
            }

            var exception = logEvent.Exception;
            var formatted = Layout.Render(logEvent);
            var template = logEvent.Message;

            if (logEvent.Level >= Options.MinLogLevelForEvent)
            {
                var evt = new SentryEvent(exception)
                {
                    Sdk ={
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
            if (logEvent.Level >= Options.MinimumBreadcrumbLevel)
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



        private IEnumerable<KeyValuePair<string, object>> GetLoggingEventProperties(LogEventInfo logEvent)
        {
            var eventProperties = new Dictionary<string, object>();

            if (logEvent.HasProperties)
            {
                eventProperties = logEvent.Properties.ToDictionary(x => x.Key.ToString(), x => x.Value);
            }

            if (ContextProperties.Count > 0)
            {
                foreach (var item in ContextProperties.ToDictionary(p => p.Name, p => p.Layout?.Render(logEvent)))
                {
                    eventProperties.Add(item.Key, item.Value);
                }
            }

            return eventProperties;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            _sdkDisposable?.Dispose();
        }
    }
}
