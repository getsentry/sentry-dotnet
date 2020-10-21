using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Sentry.Extensibility;
using Sentry.PlatformAbstractions;
using Sentry.Protocol;
using Sentry.Reflection;
using OperatingSystem = Sentry.Protocol.OperatingSystem;
using Runtime = Sentry.Protocol.Runtime;

namespace Sentry.Internal
{
    internal class MainSentryEventProcessor : ISentryEventProcessor
    {
        private readonly Lazy<string?> _release = new Lazy<string?>(ReleaseLocator.GetCurrent);

        private readonly Lazy<Runtime?> _runtime = new Lazy<Runtime?>(() =>
        {
            var current = PlatformAbstractions.Runtime.Current;
            return current != null
                   ? new Runtime
                   {
                       Name = current.Name,
                       Version = current.Version,
                       RawDescription = current.Raw
                   }
                   : null;
        });

        private static readonly SdkVersion NameAndVersion
            = typeof(ISentryClient).Assembly.GetNameAndVersion();

        private static readonly string ProtocolPackageName = "nuget:" + NameAndVersion.Name;

        private readonly SentryOptions _options;
        internal Func<ISentryStackTraceFactory> SentryStackTraceFactoryAccessor { get; }

        internal string? Release => _release.Value;
        internal Runtime? Runtime => _runtime.Value;

        public MainSentryEventProcessor(
            SentryOptions options,
            Func<ISentryStackTraceFactory> sentryStackTraceFactoryAccessor)
        {
            Debug.Assert(options != null);
            Debug.Assert(sentryStackTraceFactoryAccessor != null);
            _options = options;
            SentryStackTraceFactoryAccessor = sentryStackTraceFactoryAccessor;
        }

        public SentryEvent Process(SentryEvent @event)
        {
            _options.DiagnosticLogger?.LogDebug("Running main event processor on: Event {0}", @event.EventId);

            if (!@event.Contexts.ContainsKey(Runtime.Type) && Runtime != null)
            {
                @event.Contexts[Runtime.Type] = Runtime;
            }

            if (!@event.Contexts.ContainsKey(OperatingSystem.Type))
            {
                // RuntimeInformation.OSDescription is throwing on Mono 5.12
                if (!PlatformAbstractions.Runtime.Current.IsMono())
                {
                    @event.Contexts.OperatingSystem.RawDescription = RuntimeInformation.OSDescription;
                }
            }

            if (TimeZoneInfo.Local is { } timeZoneInfo)
            {
                @event.Contexts.Device.Timezone = timeZoneInfo;
            }

            const string currentUiCultureKey = "CurrentUICulture";
            if (!@event.Contexts.ContainsKey(currentUiCultureKey)
                && CultureInfoToDictionary(CultureInfo.CurrentUICulture) is { } currentUiCultureMap)
            {
                @event.Contexts[currentUiCultureKey] = currentUiCultureMap;
            }

            const string cultureInfoKey = "CurrentCulture";
            if (!@event.Contexts.ContainsKey(cultureInfoKey)
                && CultureInfoToDictionary(CultureInfo.CurrentCulture) is { } currentCultureMap)
            {
                @event.Contexts[cultureInfoKey] = currentCultureMap;
            }

            @event.Platform = Protocol.Constants.Platform;

            if (@event.Sdk != null)
            {
                // SDK Name/Version might have be already set by an outer package
                // e.g: ASP.NET Core can set itself as the SDK
                if (@event.Sdk.Version == null && @event.Sdk.Name == null)
                {
                    @event.Sdk.Name = Constants.SdkName;
                    @event.Sdk.Version = NameAndVersion.Version;
                }

                if (NameAndVersion.Version != null)
                {
                    @event.Sdk.AddPackage(ProtocolPackageName, NameAndVersion.Version);
                }
            }

            // Report local user if opt-in PII, no user was already set to event and feature not opted-out:
            if (_options.SendDefaultPii && _options.IsEnvironmentUser && !@event.HasUser())
            {
                @event.User.Username = Environment.UserName;
            }

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

            // Recommendation: The 'Environment' setting should always be set
            //                 with a default fallback.
            if (string.IsNullOrWhiteSpace(@event.Environment))
            {
                var foundEnvironment = EnvironmentLocator.Locate();
                @event.Environment = string.IsNullOrWhiteSpace(foundEnvironment)
                    ? string.IsNullOrWhiteSpace(_options.Environment)
                        ? Constants.ProductionEnvironmentSetting
                        : _options.Environment
                    : foundEnvironment;
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
                    @event.Modules[asmName.Name] = asmName.Version.ToString();
                }
            }

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
