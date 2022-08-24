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
        public SentryId Id { get; }

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
        public SessionDetails? SessionDetails { get; }

        private int _errorCount;

        /// <inheritdoc />
        // Sentry's implementation changed to only care whether it's 0 or 1 (has error(s))
        public int ErrorCount => _errorCount;

        // Start at -1 so that the first increment puts it at 0
        private int _sequenceNumber = -1;

        internal Session(
            SentryId id,
            string? distinctId,
            DateTimeOffset startTimestamp,
            string release,
            string? environment,
            string? ipAddress,
            string? userAgent,
            SessionDetails? sessionDetails)
        {
            Id = id;
            DistinctId = distinctId;
            StartTimestamp = startTimestamp;
            Release = release;
            Environment = environment;
            IpAddress = ipAddress;
            UserAgent = userAgent;
            SessionDetails = sessionDetails;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Session"/>.
        /// </summary>
        public Session(string? distinctId, string release, string? environment, SessionDetails? sessionDetails)
            : this(
                SentryId.Create(),
                distinctId,
                DateTimeOffset.Now,
                release,
                environment,
                null,
                null,
                sessionDetails)
        {
        }

        /// <summary>
        /// Reports an error on the session.
        /// </summary>
        public void ReportError() => Interlocked.Increment(ref _errorCount);

        internal SessionUpdate CreateUpdate(
            bool isInitial,
            DateTimeOffset timestamp,
            SessionEndStatus? endStatus = null) =>
            new(this, isInitial, timestamp, Interlocked.Increment(ref _sequenceNumber), endStatus);
    }
}
