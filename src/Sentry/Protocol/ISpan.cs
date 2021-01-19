using System;

namespace Sentry.Protocol
{
    /// <summary>
    /// Span.
    /// </summary>
    public interface ISpan : ISpanContext, IHasTags, IHasExtra
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
        /// Starts a child span.
        /// </summary>
        ISpan StartChild(string operation);

        /// <summary>
        /// Finishes the span.
        /// </summary>
        void Finish();
    }

    /// <summary>
    /// Extensions for <see cref="ISpan"/>.
    /// </summary>
    public static class SpanExtensions
    {
        /// <summary>
        /// Starts a child span.
        /// </summary>
        public static ISpan StartChild(this ISpan span, string operation, string description)
        {
            var child = span.StartChild(operation);
            child.Description = description;

            return child;
        }

        /// <summary>
        /// Finishes the span.
        /// </summary>
        public static void Finish(this ISpan span, SpanStatus status)
        {
            span.Status = status;
            span.Finish();
        }
    }
}
