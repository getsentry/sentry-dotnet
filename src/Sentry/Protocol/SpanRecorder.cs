using System.Collections.Generic;

namespace Sentry.Protocol
{
    /// <summary>
    /// Records spans that belong to the same transaction.
    /// </summary>
    public class SpanRecorder
    {
        private const int MaxSpans = 1000;

        private readonly object _lock = new object();
        private readonly List<Span> _spans = new List<Span>();

        /// <summary>
        /// Records a span.
        /// </summary>
        public void Add(Span span)
        {
            lock (_lock)
            {
                if (_spans.Count < MaxSpans)
                {
                    _spans.Add(span);
                }
            }
        }

        /// <summary>
        /// Gets all recorded spans.
        /// </summary>
        public IReadOnlyList<Span> GetAll()
        {
            lock (_lock)
            {
                return _spans;
            }
        }
    }
}
