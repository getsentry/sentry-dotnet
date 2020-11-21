using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Sentry.Internal;
using Sentry.Protocol;
using Constants = Sentry.Protocol.Constants;

// ReSharper disable once CheckNamespace
namespace Sentry
{
    /// <summary>
    /// An event to be sent to Sentry.
    /// </summary>
    /// <seealso href="https://develop.sentry.dev/sdk/event-payloads/" />
    [DataContract]
    [DebuggerDisplay("{GetType().Name,nq}: {" + nameof(EventId) + ",nq}")]
    public class SentryEvent : IScope
    {
        /// <inheritdoc />
        public IScopeOptions? ScopeOptions { get; }

        [DataMember(Name = "modules", EmitDefaultValue = false)]
        internal IDictionary<string, string>? InternalModules { get; set; }

        /// <summary>
        /// The <see cref="System.Exception"/> used to create this event.
        /// </summary>
        /// <remarks>
        /// The information from this exception is used by the Sentry SDK
        /// to add the relevant data to the event prior to sending to Sentry.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Exception? Exception { get; }

        /// <summary>
        /// The unique identifier of this event.
        /// </summary>
        /// <remarks>
        /// Hexadecimal string representing a uuid4 value.
        /// The length is exactly 32 characters (no dashes!).
        /// </remarks>
        [DataMember(Name = "event_id", EmitDefaultValue = false)]
        public SentryId EventId { get; }

        /// <summary>
        /// Indicates when the event was created.
        /// </summary>
        /// <example>2018-04-03T17:41:36</example>
        [DataMember(Name = "timestamp", EmitDefaultValue = false)]
        public DateTimeOffset Timestamp { get; }

        /// <summary>
        /// Gets the structured message that describes this event.
        /// </summary>
        /// <remarks>
        /// This helps Sentry group events together as the grouping happens
        /// on the template message instead of the result string message.
        /// </remarks>
        /// <example>
        /// SentryMessage will have a template like: 'user {0} logged in'
        /// Or structured logging template: '{user} has logged in'
        /// </example>
        [DataMember(Name = "logentry", EmitDefaultValue = false)]
        public SentryMessage? Message { get; set; }

        /// <summary>
        /// Name of the logger (or source) of the event.
        /// </summary>
        [DataMember(Name = "logger", EmitDefaultValue = false)]
        public string? Logger { get; set; }

        /// <summary>
        /// The name of the platform.
        /// </summary>
        [DataMember(Name = "platform", EmitDefaultValue = false)]
        public string? Platform { get; set; }

        /// <summary>
        /// Identifies the host SDK from which the event was recorded.
        /// </summary>
        [DataMember(Name = "server_name", EmitDefaultValue = false)]
        public string? ServerName { get; set; }

        /// <summary>
        /// The release version of the application.
        /// </summary>
        [DataMember(Name = "release", EmitDefaultValue = false)]
        public string? Release { get; set; }

        [DataMember(Name = "exception", EmitDefaultValue = false)]
        internal SentryValues<SentryException>? SentryExceptionValues { get; set; }

        [DataMember(Name = "threads", EmitDefaultValue = false)]
        internal SentryValues<SentryThread>? SentryThreadValues { get; set; }

        /// <summary>
        /// The Sentry Exception interface.
        /// </summary>
        public IEnumerable<SentryException>? SentryExceptions
        {
            get => SentryExceptionValues?.Values ?? Enumerable.Empty<SentryException>();
            set => SentryExceptionValues = value != null ? new SentryValues<SentryException>(value) : null;
        }

        /// <summary>
        /// The Sentry Thread interface.
        /// </summary>
        /// <see href="https://develop.sentry.dev/sdk/event-payloads/threads/"/>
        public IEnumerable<SentryThread>? SentryThreads
        {
            get => SentryThreadValues?.Values ?? Enumerable.Empty<SentryThread>();
            set => SentryThreadValues = value != null ? new SentryValues<SentryThread>(value) : null;
        }

        /// <summary>
        /// A list of relevant modules and their versions.
        /// </summary>
        public IDictionary<string, string> Modules => InternalModules ??= new Dictionary<string, string>();

        /// <inheritdoc />
        [DataMember(Name = "level", EmitDefaultValue = false)]
        public SentryLevel? Level { get; set; }

        /// <inheritdoc />
        [DataMember(Name = "transaction", EmitDefaultValue = false)]
        public string? Transaction { get; set; }

        [DataMember(Name = "request", EmitDefaultValue = false)]
        private Request? _request;

        /// <inheritdoc />
        public Request Request
        {
            get => _request ??= new Request();
            set => _request = value;
        }

        [DataMember(Name = "contexts", EmitDefaultValue = false)]
        private Contexts? _contexts;

        /// <inheritdoc />
        public Contexts Contexts
        {
            get => _contexts ??= new Contexts();
            set => _contexts = value;
        }

        [DataMember(Name = "user", EmitDefaultValue = false)]
        private User? _user;

        /// <inheritdoc />
        public User User
        {
            get => _user ??= new User();
            set => _user = value;
        }

        /// <inheritdoc />
        [DataMember(Name = "environment", EmitDefaultValue = false)]
        public string? Environment { get; set; }

        /// <inheritdoc />
        [DataMember(Name = "sdk", EmitDefaultValue = false)]
        public SdkVersion Sdk { get; internal set; } = new SdkVersion();

        /// <inheritdoc />
        [DataMember(Name = "fingerprint", EmitDefaultValue = false)]
        [DontSerializeEmpty]
        public IEnumerable<string> Fingerprint { get; set; } = Enumerable.Empty<string>();

        /// <inheritdoc />
        [DataMember(Name = "breadcrumbs", EmitDefaultValue = false)]
        [DontSerializeEmpty]
        public IEnumerable<Breadcrumb> Breadcrumbs { get; } = new List<Breadcrumb>();

        /// <inheritdoc />
        [DataMember(Name = "extra", EmitDefaultValue = false)]
        [DontSerializeEmpty]
        public IReadOnlyDictionary<string, object?> Extra { get; } = new Dictionary<string, object?>();

        /// <inheritdoc />
        [DataMember(Name = "tags", EmitDefaultValue = false)]
        [DontSerializeEmpty]
        public IReadOnlyDictionary<string, string> Tags { get; } = new Dictionary<string, string>();

        /// <summary>
        /// Creates a new instance of <see cref="T:Sentry.SentryEvent" />.
        /// </summary>
        public SentryEvent() : this(null)
        {
        }

        /// <summary>
        /// Creates a Sentry event with optional Exception details and default values like Id and Timestamp.
        /// </summary>
        /// <param name="exception">The exception.</param>
        public SentryEvent(Exception? exception)
            : this(exception, null)
        {
        }

        [JsonConstructor]
        internal SentryEvent(
            Exception? exception = null,
            DateTimeOffset? timestamp = null,
            SentryId eventId = default,
            IScopeOptions? options = null)
        {
            Exception = exception;
            Timestamp = timestamp ?? DateTimeOffset.UtcNow;
            EventId = eventId != default ? eventId : (SentryId)Guid.NewGuid();
            ScopeOptions = options;
            Platform = Constants.Platform;
        }
    }
}
