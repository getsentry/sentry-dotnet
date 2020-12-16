using System;
using System.Collections.Generic;

namespace Sentry.Protocol
{
    /// <summary>
    /// Span.
    /// </summary>
    public interface ISpan : ISpanContext
    {
        /// <summary>
        /// Description.
        /// </summary>
        string? Description { get; set; }

        /// <summary>
        /// Start timestamp.
        /// </summary>
        DateTimeOffset StartTimestamp { get; }

        /// <summary>
        /// End timestamp.
        /// </summary>
        DateTimeOffset? EndTimestamp { get; }

        /// <summary>
        /// Tags.
        /// </summary>
        IReadOnlyDictionary<string, string> Tags { get; }

        /// <summary>
        /// Data.
        /// </summary>
        IReadOnlyDictionary<string, object?> Extra { get; }

        /// <summary>
        /// Starts a child span.
        /// </summary>
        ISpan StartChild(string operation);

        /// <summary>
        /// Finishes the span.
        /// </summary>
        void Finish(SpanStatus status = SpanStatus.Ok);
    }
}
