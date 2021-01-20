using Sentry.Protocol.Context;

namespace Sentry.Protocol
{
    /// <summary>
    /// Span context.
    /// </summary>
    public interface ISpanContext : ITraceContext
    {
        /// <summary>
        /// Description.
        /// </summary>
        string? Description { get; }
    }
}
