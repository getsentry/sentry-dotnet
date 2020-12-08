using System;
using System.Collections.Generic;

namespace Sentry.Protocol
{
    /// <summary>
    /// Span.
    /// </summary>
    public interface ISpan
    {
        /// <summary>
        /// Span ID.
        /// </summary>
        SentryId SpanId { get; }

        /// <summary>
        /// Parent ID.
        /// </summary>
        SentryId? ParentSpanId { get; }

        /// <summary>
        /// Trace ID.
        /// </summary>
        SentryId TraceId { get; }

        /// <summary>
        /// Start timestamp.
        /// </summary>
        DateTimeOffset StartTimestamp { get; }

        /// <summary>
        /// End timestamp.
        /// </summary>
        DateTimeOffset? EndTimestamp { get; }

        /// <summary>
        /// Operation.
        /// </summary>
        string Operation { get; }

        /// <summary>
        /// Description.
        /// </summary>
        string? Description { get; set; }

        /// <summary>
        /// Status.
        /// </summary>
        SpanStatus? Status { get; }

        /// <summary>
        /// Is sampled.
        /// </summary>
        bool IsSampled { get; set; }

        /// <summary>
        /// Tags.
        /// </summary>
        IReadOnlyDictionary<string, string> Tags { get; }

        /// <summary>
        /// Data.
        /// </summary>
        IReadOnlyDictionary<string, object> Data { get; }

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
