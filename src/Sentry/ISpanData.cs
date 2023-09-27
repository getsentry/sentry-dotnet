using Sentry.Protocol;

namespace Sentry;

/// <summary>
/// Immutable data belonging to a span.
/// </summary>
public interface ISpanData : ITraceContext, IHasTags, IHasExtra
{
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
    /// Get Sentry trace header.
    /// </summary>
    SentryTraceHeader GetTraceHeader();
}
