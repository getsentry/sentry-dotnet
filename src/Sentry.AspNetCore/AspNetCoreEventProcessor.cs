using Sentry.Extensibility;

namespace Sentry.AspNetCore;

internal class AspNetCoreEventProcessor : ISentryEventProcessor
{
    public SentryEvent Process(SentryEvent @event)
    {
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
