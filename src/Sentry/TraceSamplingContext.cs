using Sentry.Protocol;

namespace Sentry
{
    public class TraceSamplingContext
    {
        public ISpan Span { get; }

        public ISpan? ParentSpan { get; }

        public TraceSamplingContext(ISpan span, ISpan? parentSpan = null)
        {
            Span = span;
            ParentSpan = parentSpan;
        }
    }
}
