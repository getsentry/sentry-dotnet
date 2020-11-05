#if NETFX
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Sentry.Extensibility;
using Sentry.PlatformAbstractions;

namespace Sentry.PlatformAbstractions
{
    internal class NetFxInstallationsEventProcessor : ISentryEventProcessor
    {
        internal static readonly string NetFxInstallationsKey = ".NET Framework";

        private readonly Lazy<Dictionary<string, string>> _netFxInstallations =
            new Lazy<Dictionary<string, string>>(() => GetInstallationsDictionary(), LazyThreadSafetyMode.ExecutionAndPublication);

        private bool _netFxInstallationEnabled = true;

        private readonly SentryOptions _options;

        internal NetFxInstallationsEventProcessor(SentryOptions options)
        {
            Debug.Assert(options != null);
            _options = options;
        }

        internal static Dictionary<string, string> GetInstallationsDictionary()
        {
            var versionsDictionary = new Dictionary<string, string>();
            var installations = FrameworkInfo.GetInstallations();
            foreach (var profile in installations.Select(p => p.Profile).Distinct())
            {
                versionsDictionary.Add($"{NetFxInstallationsKey} {profile}",
                    string.Join(", ", installations.Where(p => p.Profile == profile)
                                        .Select(p => $"\"{p.GetVersionNumber()}\"")));
            }
            return versionsDictionary;
        }

        public SentryEvent? Process(SentryEvent @event)
        {
            if (_netFxInstallationEnabled)
            {
                if (!@event.Contexts.ContainsKey(NetFxInstallationsKey))
                {
                    try
                    {
                        @event.Contexts[NetFxInstallationsKey] = _netFxInstallations.Value;
                    }
                    catch (Exception ex)
                    {
                        _options.DiagnosticLogger?.LogError("Failed to add NetFxInstallations into event.", ex);
                        //In case of any failure, this process function will be disabled to avoid throwing exceptions for future events.
                        _netFxInstallationEnabled = false;
                    }
                }
            }
            else
            {
                _options.DiagnosticLogger.LogDebug("Process skipped due to an exception with this with the processor.");
            }
            return @event;
        }
    }
}
#endif
