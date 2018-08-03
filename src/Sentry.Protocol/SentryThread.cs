using System.Runtime.Serialization;

namespace Sentry.Protocol
{
    /// <summary>
    /// A thread running at the time of an event
    /// </summary>
    /// <see href="https://docs.sentry.io/clientdev/interfaces/threads/"/>
    [DataContract]
    public class SentryThread
    {
        /// <summary>
        /// The Id of the thread
        /// </summary>
        [DataMember(Name = "id", EmitDefaultValue = false)]
        public int? Id { get; set; }

        /// <summary>
        /// The name of the thread
        /// </summary>
        [DataMember(Name = "name", EmitDefaultValue = false)]
        public string  Name { get; set; }

        /// <summary>
        /// Whether the crash happened on this thread.
        /// </summary>
        [DataMember(Name = "crashed", EmitDefaultValue = false)]
        public bool? Crashed { get; set; }

        /// <summary>
        /// An optional flag to indicate that the thread was in the foreground.
        /// </summary>
        [DataMember(Name = "current", EmitDefaultValue = false)]
        public bool? Current { get; set; }

        /// <summary>
        /// Stack trace
        /// </summary>
        /// <see href="https://docs.sentry.io/clientdev/interfaces/stacktrace/"/>
        [DataMember(Name = "stacktrace", EmitDefaultValue = false)]
        public SentryStackTrace Stacktrace { get; set; }
    }
}
