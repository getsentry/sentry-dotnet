using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using Sentry.Protocol;

// ReSharper disable once CheckNamespace
namespace Sentry
{
    /// <summary>
    /// An event to be sent to Sentry
    /// </summary>
    /// <seealso href="https://docs.sentry.io/clientdev/attributes/" />
    /// <inheritdoc />
    [DataContract]
    [DebuggerDisplay("{GetType().Name,nq}: {" + nameof(EventId) + ",nq}")]
    public class SentryEvent : Scope
    {
        [DataMember(Name = "modules", EmitDefaultValue = false)]
        internal IImmutableDictionary<string, string> InternalModules { get; set; }

        [DataMember(Name = "event_id", EmitDefaultValue = false)]
        private string SerializableEventId => EventId.ToString("N");

        /// <summary>
        /// The unique identifier of this event
        /// </summary>
        /// <remarks>
        /// Hexadecimal string representing a uuid4 value.
        /// The length is exactly 32 characters (no dashes!)
        /// </remarks>
        public Guid EventId { get; }

        /// <summary>
        /// Gets the message that describes this event
        /// </summary>
        [DataMember(Name = "message", EmitDefaultValue = false)]
        public string Message { get; set; }

        /// <summary>
        /// Indicates when the event was created
        /// </summary>
        /// <example>2018-04-03T17:41:36</example>
        [DataMember(Name = "timestamp", EmitDefaultValue = false)]
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// Name of the logger (or source) of the event
        /// </summary>
        [DataMember(Name = "logger", EmitDefaultValue = false)]
        public string Logger { get; set; }

        /// <summary>
        /// The name of the platform
        /// </summary>
        [DataMember(Name = "platform", EmitDefaultValue = false)]
        public string Platform { get; set; }

        /// <summary>
        /// Sentry level
        /// </summary>
        [DataMember(Name = "level", EmitDefaultValue = false)]
        public SentryLevel? Level { get; set; }

        /// <summary>
        /// The culprit
        /// </summary>
        /// <remarks>
        /// This value is essentially obsolete in favor of Transaction.
        /// </remarks>
        // TODO: Delete?
        [DataMember(Name = "culprit", EmitDefaultValue = false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public string Culprit { get; set; }

        /// <summary>
        /// Identifies the host SDK from which the event was recorded.
        /// </summary>
        [DataMember(Name = "server_name", EmitDefaultValue = false)]
        public string ServerName { get; set; }

        /// <summary>
        /// The release version of the application.
        /// </summary>
        [DataMember(Name = "release", EmitDefaultValue = false)]
        public string Release { get; set; }

        [DataMember(Name = "exception", EmitDefaultValue = false)]
        internal SentryValues<SentryException> SentryExceptionValues { get; set; }

        /// <summary>
        /// The Sentry Exception interface
        /// </summary>
        public IEnumerable<SentryException> SentryExceptions
            => SentryExceptionValues?.Values ?? Enumerable.Empty<SentryException>();

        /// <summary>
        /// A list of relevant modules and their versions.
        /// </summary>
        public IImmutableDictionary<string, string> Modules
        {
            get => InternalModules ?? (InternalModules = ImmutableDictionary<string, string>.Empty);
            internal set => InternalModules = value;
        }

        internal Exception Exception { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="T:Sentry.SentryEvent" />
        /// </summary>
        /// <inheritdoc />
        public SentryEvent() : this(null)
        { }

        /// <summary>
        /// Creates a Sentry event with optional Exception details and default values like Id and Timestamp
        /// </summary>
        /// <param name="exception">The exception.</param>
        public SentryEvent(Exception exception)
            : this(exception, null)
        { }

        internal SentryEvent(
            Exception exception = null,
            DateTimeOffset? timestamp = null,
            Guid id = default)
        {
            EventId = id == default ? Guid.NewGuid() : id;

            Timestamp = timestamp ?? DateTimeOffset.UtcNow;
            Exception = exception;

            Platform = Constants.Platform;
        }
    }
}
