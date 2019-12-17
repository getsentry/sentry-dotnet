using System.Collections.Generic;
using System.Runtime.Serialization;

// ReSharper disable once CheckNamespace
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
        [DataMember(Name = "frames", EmitDefaultValue = false)]
        internal IList<SentryStackFrame> InternalFrames { get; private set; }

        /// <summary>
        /// The list of frames in the stack
        /// </summary>
        /// <remarks>
        /// The list of frames should be ordered by the oldest call first.
        /// </remarks>
        public IList<SentryStackFrame> Frames
        {
            get => InternalFrames ?? (InternalFrames = new List<SentryStackFrame>());
            set => InternalFrames = value;
        }
    }
}
