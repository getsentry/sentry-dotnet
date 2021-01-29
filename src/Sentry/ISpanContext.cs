using Sentry.Protocol;

namespace Sentry
{
    /// <summary>
    /// Span metadata.
    /// </summary>
    public interface ISpanContext : ITraceContext
    {
        /// <summary>
        /// Description.
        /// </summary>
        string? Description { get; }
    }
}
