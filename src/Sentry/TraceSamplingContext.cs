using Sentry.Protocol;

namespace Sentry
{
    /// <summary>
    /// Trace sampling context.
    /// </summary>
    public class TraceSamplingContext
    {
        /// <summary>
        /// Span.
        /// </summary>
        public ISpan Span { get; }

        /// <summary>
        /// Span's parent.
        /// </summary>
        public ISpan? ParentSpan { get; }

        /// <summary>
        /// Initializes an instance of <see cref="TraceSamplingContext"/>.
        /// </summary>
        /// <param name="span"></param>
        /// <param name="parentSpan"></param>
        public TraceSamplingContext(ISpan span, ISpan? parentSpan = null)
        {
            Span = span;
            ParentSpan = parentSpan;
        }
    }
}
