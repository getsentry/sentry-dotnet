using System.Runtime.Serialization;

namespace Sentry.Protocol
{
    /// <summary>
    /// Sentry Exception interface
    /// </summary>
    /// <see href="https://docs.sentry.io/clientdev/interfaces/exception/"/>
    [DataContract]
    public class SentryException
    {
        /// <summary>
        /// Exception Type
        /// </summary>
        [DataMember(Name = "type", EmitDefaultValue = false)]
        public string Type { get; set; }

        /// <summary>
        /// The exception value
        /// </summary>
        [DataMember(Name = "value", EmitDefaultValue = false)]
        public string Value { get; set; }

        /// <summary>
        /// The optional module, or package which the exception type lives in
        /// </summary>
        [DataMember(Name = "module", EmitDefaultValue = false)]
        public string Module { get; set; }

        /// <summary>
        /// An optional value which refers to a thread in the threads interface.
        /// </summary>
        /// <seealso href="https://docs.sentry.io/clientdev/interfaces/threads/"/>
        [DataMember(Name = "thread_id", EmitDefaultValue = false)]
        public int ThreadId { get; set; }

        /// <summary>
        /// Stack trace
        /// </summary>
        /// <see href="https://docs.sentry.io/clientdev/interfaces/stacktrace/"/>
        [DataMember(Name = "stacktrace", EmitDefaultValue = false)]
        public SentryStackTrace Stacktrace { get; set; }

        // https://docs.sentry.io/clientdev/interfaces/mechanism/
        // TODO: Mechanism 
        public object Mechanism { get; set; }
    }
}
