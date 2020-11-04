#if NETFX
using System;
using System.Collections.Generic;
using Sentry.Extensibility;
using Sentry.PlatformAbstractions;

namespace Sentry.Internal
{
    internal class NetFxInstallationsEventProcessor : ISentryEventProcessor
    {
        internal static readonly string NetFxInstallationsKey = ".NET Framework";

        private readonly Lazy<IEnumerable<FrameworkInstallation>> _netFxInstallations =
            new Lazy<IEnumerable<FrameworkInstallation>>(() => FrameworkInfo.GetInstallations());

        public SentryEvent? Process(SentryEvent @event)
        {
            if (!@event.Contexts.ContainsKey(NetFxInstallationsKey))
            {
                @event.Contexts[NetFxInstallationsKey] = _netFxInstallations.Value;
            }
            return @event;
        }
    }
}
#endif
