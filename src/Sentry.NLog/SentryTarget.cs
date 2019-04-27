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
        
        private SentryTarget(SentryNLogOptions options) : this(options, SentrySdk.Init)
        {

        }

        internal SentryTarget(
            SentryNLogOptions options,
            Func<SentryNLogOptions, IDisposable> sdkDisposable)
            : this(
                options,
                () => HubAdapter.Instance,
                sdkDisposable(options),
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

            Layout = "${message}";

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

        /// <inheritdoc/>
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

            // Even if it was sent as event, add breadcrumb so next event includes it
            if (logEvent.Level >= Options.MinimumBreadcrumbLevel)
            {
                Dictionary<string, string> data = null;
                // If this is true, an exception is being logged with no custom message
                if (exception != null && !string.IsNullOrWhiteSpace(formatted) && logEvent.Message != "{0}")
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

        private IEnumerable<KeyValuePair<string, string>> GetLoggingContextProperties(LogEventInfo logEvent)
        {
            var baseProps = base.GetContextProperties(logEvent) ?? new Dictionary<string, object>();
            var contextProps = baseProps?.ToDictionary(a => a.Key, a => a.Value.ToString());

            ICollection<KeyValuePair<string, string>> eventContextProperties = new Dictionary<string, string>();

            if (ContextProperties.Count > 0)
            {
                foreach (var item in ContextProperties.ToDictionary(a => a.Name, a => a.Layout?.Render(logEvent)).Concat(contextProps))
                {
                    eventContextProperties.Add(item);
                }
            }

            return eventContextProperties;
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

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            _sdkDisposable?.Dispose();
        }
    }
}
