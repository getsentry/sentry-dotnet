using System;
using System.Threading;

namespace Sentry
{
    public class Session
    {
        public string Id { get; }

        public string? DistinctId { get; }

        public DateTimeOffset Timestamp { get; }

        public string Release { get; }

        public string? Environment { get; }

        public string? IpAddress { get; }

        public string? UserAgent { get; }

        public SessionEndState EndState { get; private set; }

        private int _errorCount;
        public int ErrorCount => _errorCount;

        internal Session(
            string id,
            string? distinctId,
            DateTimeOffset timestamp,
            string release,
            string? environment,
            string? ipAddress,
            string? userAgent)
        {
            Id = id;
            DistinctId = distinctId;
            Timestamp = timestamp;
            Release = release;
            Environment = environment;
            IpAddress = ipAddress;
            UserAgent = userAgent;
        }

        public Session(string? distinctId, string release, string? environment)
            : this(
                Guid.NewGuid().ToString(),
                distinctId,
                DateTimeOffset.Now,
                release,
                environment,
                null,
                null)
        {
        }

        public void End(SessionEndState state) => EndState = state;

        public void ReportError() => Interlocked.Increment(ref _errorCount);

        public SessionSnapshot CreateSnapshot(bool isInitial) => new(this, isInitial);
    }
}
