using System;

namespace Sentry
{
    /// <summary>
    /// Session metadata.
    /// </summary>
    public interface ISession
    {
        /// <summary>
        /// Session auto-generated ID.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Session distinct ID.
        /// </summary>
        string? DistinctId { get; }

        /// <summary>
        /// Session start timestamp.
        /// </summary>
        DateTimeOffset StartTimestamp { get; }

        /// <summary>
        /// Release.
        /// </summary>
        string Release { get; }

        /// <summary>
        /// Environment.
        /// </summary>
        string? Environment { get; }

        /// <summary>
        /// IP address of the user.
        /// </summary>
        string? IpAddress { get; }

        /// <summary>
        /// User agent.
        /// </summary>
        string? UserAgent { get; }

        /// <summary>
        /// Status with which the session was ended.
        /// </summary>
        SessionEndStatus? EndStatus { get; }

        /// <summary>
        /// Reported error count.
        /// </summary>
        int ErrorCount { get; }
    }
}
