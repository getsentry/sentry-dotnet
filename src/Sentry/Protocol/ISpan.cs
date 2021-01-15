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
        ISpan StartChild();

        /// <summary>
        /// Finishes the span.
        /// </summary>
        void Finish(SpanStatus status = SpanStatus.Ok);
    }

    /// <summary>
    /// Extensions for <see cref="ISpan"/>.
    /// </summary>
    public static class SpanExtensions
    {
        /// <summary>
        /// Starts a child span.
        /// </summary>
        public static ISpan StartChild(this ISpan span, string operation)
        {
            var child = span.StartChild();
            child.Operation = operation;

            return child;
        }

        /// <summary>
        /// Starts a child span.
        /// </summary>
        public static ISpan StartChild(this ISpan span, string operation, string description)
        {
            var child = span.StartChild(operation);
            child.Description = description;

            return child;
        }
    }
}
