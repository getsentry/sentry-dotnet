using System;

namespace Sentry.Protocol
{
    /// <summary>
    /// The identifier of an event in Sentry
    /// </summary>
    public readonly struct SentryId
    {
        private readonly Guid _eventId;

        /// <summary>
        /// An empty sentry id
        /// </summary>
        public static readonly SentryId Empty = Guid.Empty;

        /// <summary>
        /// Creates a new instance of a Sentry Id
        /// </summary>
        /// <param name="guid"></param>
        public SentryId(Guid guid) => _eventId = guid;

        /// <summary>
        /// Sentry Id in the format Sentry recognizes
        /// </summary>
        /// <remarks>
        /// Default <see cref="ToString"/> of <see cref="Guid"/> includes
        /// dashes which sentry doesn't expect when searching events.
        /// </remarks>
        /// <returns>String representation of the event id.</returns>
        public override string ToString() => _eventId.ToString("n");

        /// <summary>
        /// The <see cref="Guid"/> from the <see cref="SentryId"/>
        /// </summary>
        /// <param name="sentryId"></param>
        public static implicit operator Guid(SentryId sentryId) => sentryId._eventId;

        /// <summary>
        /// A <see cref="SentryId"/> from a <see cref="Guid"/>
        /// </summary>
        /// <param name="guid"></param>
        public static implicit operator SentryId(Guid guid) => new SentryId(guid);
    }
}
