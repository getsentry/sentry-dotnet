using System;
using System.Threading;

namespace Sentry
{
    /// <summary>
    /// Sentry session.
    /// </summary>
    // https://develop.sentry.dev/sdk/sessions
    public class Session
    {
        /// <summary>
        /// Auto-generated ID.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Distinct ID.
        /// </summary>
        public string? DistinctId { get; }

        /// <summary>
        /// Start timestamp.
        /// </summary>
        public DateTimeOffset Timestamp { get; }

        /// <summary>
        /// Release.
        /// </summary>
        public string Release { get; }

        /// <summary>
        /// Environment.
        /// </summary>
        public string? Environment { get; }

        /// <summary>
        /// IP Address.
        /// </summary>
        public string? IpAddress { get; }

        /// <summary>
        /// User agent.
        /// </summary>
        public string? UserAgent { get; }

        /// <summary>
        /// End state.
        /// </summary>
        public SessionEndState EndState { get; private set; }

        private int _errorCount;

        /// <summary>
        /// Reported error count.
        /// </summary>
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

        /// <summary>
        /// Initializes a new instance of <see cref="Session"/>.
        /// </summary>
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

        /// <summary>
        /// Reports an error on the session.
        /// </summary>
        public void ReportError() => Interlocked.Increment(ref _errorCount);

        /// <summary>
        /// Transitions the session to ended state.
        /// </summary>
        public void End(SessionEndState state) => EndState = state;

        /// <summary>
        /// Creates a snapshot of this session.
        /// </summary>
        public SessionUpdate CreateSnapshot(bool isInitial) => new(this, isInitial);
    }
}
