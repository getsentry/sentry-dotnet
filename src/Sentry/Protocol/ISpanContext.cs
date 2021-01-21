using Sentry.Protocol.Context;

namespace Sentry.Protocol
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
