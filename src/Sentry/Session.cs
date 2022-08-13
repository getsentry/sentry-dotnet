using System;
using System.Threading;
using Sentry.Internal;

namespace Sentry
{
    /// <summary>
    /// Sentry session.
    /// </summary>
    // https://develop.sentry.dev/sdk/sessions
    public class Session : ISession, IHasReadOnlyDistribution
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
        public string? Distribution { get; }

        /// <inheritdoc />
        public string? Environment { get; }

        /// <inheritdoc />
        public string? IpAddress { get; }

        /// <inheritdoc />
        public string? UserAgent { get; }

        private int _errorCount;

        /// <inheritdoc />
        // Sentry's implementation changed to only care whether it's 0 or 1 (has error(s))
        public int ErrorCount => _errorCount;

        // Start at -1 so that the first increment puts it at 0
        private int _sequenceNumber = -1;

        internal Session(SentryId id,
            string? distinctId,
            DateTimeOffset startTimestamp,
            string release,
            string? distribution,
            string? environment,
            string? ipAddress,
            string? userAgent)
        {
            Id = id;
            DistinctId = distinctId;
            StartTimestamp = startTimestamp;
            Release = release;
            Distribution = distribution;
            Environment = environment;
            IpAddress = ipAddress;
            UserAgent = userAgent;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Session"/>.
        /// </summary>
        public Session(string? distinctId, string release, string? environment)
            : this(distinctId, release, distribution: null, environment)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Session"/>.
        /// </summary>
        public Session(string? distinctId, string release, string? distribution, string? environment)
            : this(
                SentryId.Create(),
                distinctId,
                DateTimeOffset.Now,
                release,
                distribution,
                environment,
                null,
                null)
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
