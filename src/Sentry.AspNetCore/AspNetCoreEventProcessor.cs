using System;
using Sentry.Extensibility;
using Sentry.Protocol;
using OperatingSystem = Sentry.Protocol.OperatingSystem;

namespace Sentry.AspNetCore
{
    internal class AspNetCoreEventProcessor : ISentryEventProcessor
    {
        public SentryEvent Process(SentryEvent @event)
        {
            // Move 'runtime' under key 'server-runtime' as User-Agent parsing done at
            // Sentry will represent the client's
            if (@event.Contexts.TryRemove(Runtime.Type, out var runtime))
            {
                @event.Contexts["server-runtime"] = runtime;
            }

            if (@event.Contexts.TryRemove(OperatingSystem.Type, out var os))
            {
                @event.Contexts["server-os"] = os;
            }

            @event.ServerName = Environment.MachineName;

            return @event;
        }
    }
}
