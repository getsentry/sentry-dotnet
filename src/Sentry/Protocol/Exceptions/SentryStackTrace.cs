using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

// ReSharper disable once CheckNamespace
namespace Sentry.Protocol
{
    /// <summary>
    /// Sentry Stacktrace interface.
    /// </summary>
    /// <remarks>
    /// A stacktrace contains a list of frames, each with various bits (most optional) describing the context of that frame.
    /// Frames should be sorted from oldest to newest.
    /// </remarks>
    /// <see href="https://develop.sentry.dev/sdk/event-payloads/stacktrace/"/>
    public class SentryStackTrace : IJsonSerializable
    {
        internal IList<SentryStackFrame>? InternalFrames { get; private set; }

        /// <summary>
        /// The list of frames in the stack.
        /// </summary>
        /// <remarks>
        /// The list of frames should be ordered by the oldest call first.
        /// </remarks>
        public IList<SentryStackFrame> Frames
        {
            get => InternalFrames ??= new List<SentryStackFrame>();
            set => InternalFrames = value;
        }

        public void WriteTo(Utf8JsonWriter writer)
        {
            if (InternalFrames is {} frames && frames.Any())
            {
                writer.WriteStartArray("frames");

                foreach (var frame in frames)
                {
                    writer.WriteSerializableValue(frame);
                }

                writer.WriteEndArray();
            }
        }
    }
}
