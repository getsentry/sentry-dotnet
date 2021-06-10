using System;
using System.Threading;

namespace Sentry
{
    /// <summary>
    /// Sentry session.
    /// </summary>
    // https://develop.sentry.dev/sdk/sessions
    public class Session : ISession
    {
        /// <inheritdoc />
        public string Id { get; }

        /// <inheritdoc />
        public string? DistinctId { get; }

        /// <inheritdoc />
        public DateTimeOffset StartTimestamp { get; }

        /// <inheritdoc />
        public string Release { get; }

        /// <inheritdoc />
        public string? Environment { get; }

        /// <inheritdoc />
        public string? IpAddress { get; }

        /// <inheritdoc />
        public string? UserAgent { get; }

        /// <inheritdoc />
        public SessionEndStatus? EndStatus { get; private set; }

        private int _errorCount;

        /// <inheritdoc />
        public int ErrorCount => _errorCount;

        internal Session(
            string id,
            string? distinctId,
            DateTimeOffset startTimestamp,
            string release,
            string? environment,
            string? ipAddress,
            string? userAgent)
        {
            Id = id;
            DistinctId = distinctId;
            StartTimestamp = startTimestamp;
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
        public void End(SessionEndStatus status) => EndStatus = status;

        /// <summary>
        /// Creates an update of this session.
        /// </summary>
        public SessionUpdate CreateUpdate(bool isInitial) => new(this, isInitial);
    }
}
