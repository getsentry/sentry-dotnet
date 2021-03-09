using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Sentry.Extensibility;

namespace Sentry.Internal
{
    internal class MainSentryEventProcessor : ISentryEventProcessor
    {
        internal const string CultureInfoKey = "Current Culture";
        internal const string CurrentUiCultureKey = "Current UI Culture";

        private readonly Enricher _enricher;

        private readonly Lazy<string?> _release;

        private readonly SentryOptions _options;
        internal Func<ISentryStackTraceFactory> SentryStackTraceFactoryAccessor { get; }

        internal string? Release => _release.Value;

        public MainSentryEventProcessor(
            SentryOptions options,
            Func<ISentryStackTraceFactory> sentryStackTraceFactoryAccessor,
            Lazy<string?>? lazyRelease = null)
        {
            _options = options;
            SentryStackTraceFactoryAccessor = sentryStackTraceFactoryAccessor;
            _release = lazyRelease ?? new Lazy<string?>(ReleaseLocator.GetCurrent);

            _enricher = new Enricher(options);
        }

        public SentryEvent Process(SentryEvent @event)
        {
            _options.DiagnosticLogger?.LogDebug("Running main event processor on: Event {0}", @event.EventId);

            if (TimeZoneInfo.Local is { } timeZoneInfo)
            {
                @event.Contexts.Device.Timezone = timeZoneInfo;
            }

            IDictionary<string, string>? cultureInfoMapped = null;
            if (!@event.Contexts.ContainsKey(CultureInfoKey)
                && CultureInfoToDictionary(CultureInfo.CurrentCulture) is { } currentCultureMap)
            {
                cultureInfoMapped = currentCultureMap;
                @event.Contexts[CultureInfoKey] = currentCultureMap;
            }

            if (!@event.Contexts.ContainsKey(CurrentUiCultureKey)
                && CultureInfoToDictionary(CultureInfo.CurrentUICulture) is { } currentUiCultureMap
                && (cultureInfoMapped is null || currentUiCultureMap.Any(p => !cultureInfoMapped.Contains(p))))
            {
                @event.Contexts[CurrentUiCultureKey] = currentUiCultureMap;
            }

            @event.Platform = Sentry.Constants.Platform;

            if (@event.ServerName == null)
            {
                // Value set on the options take precedence over device name.
                if (!string.IsNullOrEmpty(_options.ServerName))
                {
                    @event.ServerName = _options.ServerName;
                }
                else if (_options.SendDefaultPii)
                {
                    @event.ServerName = Environment.MachineName;
                }
            }

            if (@event.Level == null)
            {
                @event.Level = SentryLevel.Error;
            }

            if (@event.Release == null)
            {
                @event.Release = _options.Release ?? Release;
            }

            if (@event.Exception == null)
            {
                var stackTrace = SentryStackTraceFactoryAccessor().Create(@event.Exception);
                if (stackTrace != null)
                {
                    var thread = new SentryThread
                    {
                        Crashed = false,
                        Current = true,
                        Name = Thread.CurrentThread.Name,
                        Id = Thread.CurrentThread.ManagedThreadId,
                        Stacktrace = stackTrace
                    };

                    @event.SentryThreads = @event.SentryThreads?.Any() == true
                        ? new List<SentryThread>(@event.SentryThreads) { thread }
                        : new[] { thread }.AsEnumerable();
                }
            }

            if (_options.ReportAssemblies)
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (assembly.IsDynamic)
                    {
                        continue;
                    }

                    var asmName = assembly.GetName();
                    if (asmName.Name is not null && asmName.Version is not null)
                    {
                        @event.Modules[asmName.Name] = asmName.Version.ToString();
                    }
                }
            }

            // Run enricher to fill in the gaps
            _enricher.Apply(@event);

            return @event;
        }

        private static IDictionary<string, string>? CultureInfoToDictionary(CultureInfo cultureInfo)
        {
            var dic = new Dictionary<string, string>();

            if (!string.IsNullOrWhiteSpace(cultureInfo.Name))
            {
                dic.Add("Name", cultureInfo.Name);
            }
            if (!string.IsNullOrWhiteSpace(cultureInfo.DisplayName))
            {
                dic.Add("DisplayName", cultureInfo.DisplayName);
            }
            if (cultureInfo.Calendar is { } cal)
            {
                dic.Add("Calendar", cal.GetType().Name);
            }

            return dic.Count > 0 ? dic : null;
        }
    }
}
