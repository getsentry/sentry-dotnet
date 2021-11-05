using System.Linq;

namespace Sentry
{
    /// <summary>
    /// Extension methods for <see cref="SentryEvent"/>
    /// </summary>
    public static class SentryEventExtensions
    {
        internal static bool IsErrored(this SentryEvent @event)
            => @event.Level >= SentryLevel.Error
               || @event.Exception is not null
               || @event.SentryExceptionValues?.Values.Any() == true;
    }
}
