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

            // Not PII as this is running on a server
            if (@event.ServerName == null)
            {
                // ServerName from the environment machine name is set by the Sentry base package.
                // That is guarded by the SendDefaultPii since the SDK can be used in desktop apps.
                // For ASP.NET Core apps, we always set the machine name if not explicitly set by the user.
                @event.ServerName = Environment.MachineName;
            }

            return @event;
        }
    }
}
