using System.Collections.Generic;
using System.Runtime.Serialization;

// ReSharper disable once CheckNamespace
namespace Sentry.Protocol
{
    /// <summary>
    /// Sentry Exception interface.
    /// </summary>
    /// <see href="https://develop.sentry.dev/sdk/event-payloads/exception"/>
    [DataContract]
    public class SentryException
    {
        // Not serialized since not part of the protocol yet.
        // Used by Sentry SDK though to transfer data from Exception.Data to Event.Data when parsing.
        internal Dictionary<string, object?>? InternalData { get; private set; }

        /// <summary>
        /// Exception Type.
        /// </summary>
        [DataMember(Name = "type", EmitDefaultValue = false)]
        public string? Type { get; set; }

        /// <summary>
        /// The exception value.
        /// </summary>
        [DataMember(Name = "value", EmitDefaultValue = false)]
        public string? Value { get; set; }

        /// <summary>
        /// The optional module, or package which the exception type lives in.
        /// </summary>
        [DataMember(Name = "module", EmitDefaultValue = false)]
        public string? Module { get; set; }

        /// <summary>
        /// An optional value which refers to a thread in the threads interface.
        /// </summary>
        /// <seealso href="https://develop.sentry.dev/sdk/event-payloads/threads/"/>
        /// <seealso cref="SentryThread"/>
        [DataMember(Name = "thread_id", EmitDefaultValue = false)]
        public int ThreadId { get; set; }

        /// <summary>
        /// Stack trace.
        /// </summary>
        /// <see href="https://develop.sentry.dev/sdk/event-payloads/stacktrace/"/>
        [DataMember(Name = "stacktrace", EmitDefaultValue = false)]
        public SentryStackTrace? Stacktrace { get; set; }

        /// <summary>
        /// An optional mechanism that created this exception.
        /// </summary>
        /// <see href="https://develop.sentry.dev/sdk/event-payloads/exception/#exception-mechanism"/>
        [DataMember(Name = "mechanism", EmitDefaultValue = false)]
        public Mechanism? Mechanism { get; set; }

        /// <summary>
        /// Arbitrary extra data that related to this error
        /// </summary>
        /// <remarks>
        /// The protocol does not yet support data at this level.
        /// For this reason this property is not serialized.
        /// The data is moved to the event level on Extra until such support is added
        /// </remarks>
        public IDictionary<string, object?> Data => InternalData ??= new Dictionary<string, object?>();
    }
}
