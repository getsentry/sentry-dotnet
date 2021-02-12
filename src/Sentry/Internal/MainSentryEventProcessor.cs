using System;
using System.Collections.Generic;
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
        internal const string CultureInfoKey = "Current Culture";
        internal const string CurrentUiCultureKey = "Current UI Culture";

        private readonly Lazy<string?> _release;

        private readonly Lazy<Runtime> _runtime = new(() =>
        {
            var current = PlatformAbstractions.Runtime.Current;
            return new Runtime
            {
                Name = current.Name,
                Version = current.Version,
                RawDescription = current.Raw
            };
        });

        internal static readonly SdkVersion NameAndVersion
            = typeof(ISentryClient).Assembly.GetNameAndVersion();

        internal static readonly string ProtocolPackageName = "nuget:" + NameAndVersion.Name;

        private readonly SentryOptions _options;
        internal Func<ISentryStackTraceFactory> SentryStackTraceFactoryAccessor { get; }

        internal string? Release => _release.Value;
        internal Runtime Runtime => _runtime.Value;

        /// <summary>
        /// A flag that tells the endpoint to figure out the user ip.
        /// </summary>
        internal string UserIpServerInferred = "{{auto}}";

        public MainSentryEventProcessor(
            SentryOptions options,
            Func<ISentryStackTraceFactory> sentryStackTraceFactoryAccessor,
            Lazy<string?>? lazyRelease = null)
        {
            _options = options;
            SentryStackTraceFactoryAccessor = sentryStackTraceFactoryAccessor;
            _release = lazyRelease ?? new Lazy<string?>(ReleaseLocator.GetCurrent);
        }

        public SentryEvent Process(SentryEvent @event)
        {
            _options.DiagnosticLogger?.LogDebug("Running main event processor on: Event {0}", @event.EventId);

            if (!@event.Contexts.ContainsKey(Runtime.Type))
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

            // Report local user if opt-in PII, no user was already set to event and feature not opted-out:
            if (_options.SendDefaultPii)
            {
                if (_options.IsEnvironmentUser && !@event.HasUser())
                {
                    @event.User.Username = Environment.UserName;
                }
                @event.User.IpAddress ??= UserIpServerInferred;
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
                    if (asmName.Name is not null && asmName.Version is not null)
                    {
                        @event.Modules[asmName.Name] = asmName.Version.ToString();
                    }
                }
            }

            _options.ApplyDefaultTags(@event);

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
