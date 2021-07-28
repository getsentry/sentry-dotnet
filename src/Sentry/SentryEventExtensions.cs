using System.Linq;

namespace Sentry
{
    /// <summary>
    /// Extension methods for <see cref="SentryEvent"/>
    /// </summary>
    public static class SentryEventExtensions
    {
        internal static bool IsErrored(this SentryEvent @event)
            => @event.Exception is not null || @event.SentryExceptions?.Any() == true;
    }
}
