using System;
using System.Collections.Generic;
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
    public class SentryEvent : BaseScope
    {
        [DataMember(Name = "modules", EmitDefaultValue = false)]
        internal IDictionary<string, string> InternalModules { get; set; }

        [DataMember(Name = "event_id", EmitDefaultValue = false)]
        private string SerializableEventId => EventId.ToString();

        /// <summary>
        /// The <see cref="System.Exception"/> used to create this event.
        /// </summary>
        /// <remarks>
        /// The information from this exception is used by the Sentry SDK
        /// to add the relevant data to the event prior to sending to Sentry.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Exception Exception { get; }

        /// <summary>
        /// The unique identifier of this event
        /// </summary>
        /// <remarks>
        /// Hexadecimal string representing a uuid4 value.
        /// The length is exactly 32 characters (no dashes!)
        /// </remarks>
        public SentryId EventId { get; }

        /// <summary>
        /// Indicates when the event was created
        /// </summary>
        /// <example>2018-04-03T17:41:36</example>
        [DataMember(Name = "timestamp", EmitDefaultValue = false)]
        public DateTimeOffset Timestamp { get; }

        /// <summary>
        /// Gets the message that describes this event
        /// </summary>
        [DataMember(Name = "message", EmitDefaultValue = false)]
        public string Message { get; set; }

        /// <summary>
        /// Gets the structured message that describes this event
        /// </summary>
        /// <remarks>
        /// This helps Sentry group events together as the grouping happens
        /// on the template message instead of the result string message.
        /// </remarks>
        /// <example>
        /// LogEntry will have a template like: 'user {0} logged in'
        /// Or structured logging template '{user} has logged in'
        /// </example>
        [DataMember(Name = "logentry", EmitDefaultValue = false)]
        public LogEntry LogEntry { get; set; }

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

        [DataMember(Name = "threads", EmitDefaultValue = false)]
        internal SentryValues<SentryThread> SentryThreadValues { get; set; }

        /// <summary>
        /// The Sentry Exception interface
        /// </summary>
        public IEnumerable<SentryException> SentryExceptions
        {
            get => SentryExceptionValues?.Values ?? Enumerable.Empty<SentryException>();
            set => SentryExceptionValues = value == null ? null : new SentryValues<SentryException>(value);
        }

        /// <summary>
        /// The Sentry Thread interface
        /// </summary>
        /// <see href="https://docs.sentry.io/clientdev/interfaces/threads/"/>
        public IEnumerable<SentryThread> SentryThreads
        {
            get => SentryThreadValues?.Values ?? Enumerable.Empty<SentryThread>();
            set => SentryThreadValues = value == null ? null : new SentryValues<SentryThread>(value);
        }

        /// <summary>
        /// A list of relevant modules and their versions.
        /// </summary>
        public IDictionary<string, string> Modules => InternalModules ?? (InternalModules = new Dictionary<string, string>());

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
            Guid id = default,
            IScopeOptions options = null)
            : base (options)
        {
            EventId = id == default ? Guid.NewGuid() : id;

            Timestamp = timestamp ?? DateTimeOffset.UtcNow;
            Exception = exception;

            Platform = Constants.Platform;
        }
    }
}
