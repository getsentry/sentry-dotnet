using System;
using Microsoft.Extensions.Options;
using Sentry.Extensibility;
using Sentry.Protocol;
using OperatingSystem = Sentry.Protocol.OperatingSystem;

namespace Sentry.AspNetCore
{
    internal class AspNetCoreEventProcessor : ISentryEventProcessor
    {
        private readonly SentryAspNetCoreOptions _options;

        public AspNetCoreEventProcessor(IOptions<SentryAspNetCoreOptions> options)
            => _options = options?.Value;

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
            @event.ServerName = Environment.MachineName;

            return @event;
        }
    }
}
