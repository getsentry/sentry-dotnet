using System;

namespace Sentry.Infrastructure
{
    public sealed class SystemClock : ISystemClock
    {
        public static readonly SystemClock Clock = new SystemClock();

        public DateTimeOffset GetUtcNow() => DateTimeOffset.UtcNow;
    }
}
