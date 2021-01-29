using System;

namespace Sentry
{
    /// <summary>
    /// Span.
    /// </summary>
    public interface ISpan : ISpanContext, IHasTags, IHasExtra
    {
        /// <summary>
        /// Span description.
        /// </summary>
        // 'new' because it adds a setter.
        new string? Description { get; set; }

        /// <summary>
        /// Span operation.
        /// </summary>
        // 'new' because it adds a setter.
        new string Operation { get; set; }

        /// <summary>
        /// Span status.
        /// </summary>
        // 'new' because it adds a setter.
        new SpanStatus? Status { get; set; }

        /// <summary>
        /// Start timestamp.
        /// </summary>
        DateTimeOffset StartTimestamp { get; }

        /// <summary>
        /// End timestamp.
        /// </summary>
        DateTimeOffset? EndTimestamp { get; }

        /// <summary>
        /// Whether the span is finished.
        /// </summary>
        bool IsFinished { get; }

        /// <summary>
        /// Starts a child span.
        /// </summary>
        ISpan StartChild(string operation);

        /// <summary>
        /// Finishes the span.
        /// </summary>
        void Finish();

        /// <summary>
        /// Get Sentry trace header.
        /// </summary>
        SentryTraceHeader GetTraceHeader();
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
            span.Finish();
            span.Status = status;
        }
    }
}
