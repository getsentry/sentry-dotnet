using System;

namespace Sentry.Infrastructure
{
    /// <summary>
    /// Implementation of <see cref="ISystemClock"/> to help testability.
    /// </summary>
    /// <seealso cref="Sentry.Infrastructure.ISystemClock" />
    public sealed class SystemClock : ISystemClock
    {
        /// <summary>
        /// System clock singleton.
        /// </summary>
        public static readonly SystemClock Clock = new();

        /// <summary>
        /// Gets the current time in UTC.
        /// </summary>
        /// <remarks>
        /// Used for testability, calls: DateTimeOffset.UtcNow
        /// </remarks>
        public DateTimeOffset GetUtcNow() => DateTimeOffset.UtcNow;
    }
}
