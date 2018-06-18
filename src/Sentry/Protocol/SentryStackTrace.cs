using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Sentry.Protocol
{
    /// <summary>
    /// Sentry Stacktrace interface
    /// </summary>
    /// <remarks>
    /// A stacktrace contains a list of frames, each with various bits (most optional) describing the context of that frame. Frames should be sorted from oldest to newest.
    /// </remarks>
    /// <see href="https://docs.sentry.io/clientdev/interfaces/stacktrace/"/>
    [DataContract]
    public class SentryStackTrace
    {
        /// <summary>
        /// The list of frames in the stack
        /// </summary>
        /// <remarks>
        /// The list of frames should be ordered by the oldest call first.
        /// </remarks>
        [DataMember(Name = "frames", EmitDefaultValue = false)]
        public IEnumerable<SentryStackFrame> Frames { get; set; }
    }
}
