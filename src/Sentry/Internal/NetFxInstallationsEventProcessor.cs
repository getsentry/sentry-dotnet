#if NETFX
using System.Collections.Generic;
using Sentry.Extensibility;
using Sentry.PlatformAbstractions;

namespace Sentry.Internal
{
    internal class NetFxInstallationsEventProcessor : ISentryEventProcessor
    {
        internal static readonly string NetFxInstallationsKey = ".NET Framework";

        private readonly IEnumerable<FrameworkInstallation> _netFxInstallations = FrameworkInfo.GetInstallations();

        public SentryEvent? Process(SentryEvent @event)
        {
            if (!@event.Contexts.ContainsKey(NetFxInstallationsKey))
            {
                @event.Contexts[NetFxInstallationsKey] = _netFxInstallations;
            }
            return @event;
        }
    }
}
#endif
