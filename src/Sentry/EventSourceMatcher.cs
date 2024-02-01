#if !__MOBILE__
using System.Diagnostics.Tracing;

namespace Sentry;

/// <summary>
/// Configures which EventSource Sentry should listen to
/// </summary>
/// <param name="Pattern">A SubstringOrRegexPattern matching any EventSource names that Sentry should listen to</param>
/// <param name="Level">The minimum <see cref="EventLevel"/> required before Counters will be sent to Sentry</param>
/// <remarks>
/// This type has implicit conversions from <see cref="SubstringOrRegexPattern"/>, string, (string, EventLevel) and
/// (SubstringOrRegexPattern, EventLevel)
/// </remarks>
public sealed record EventSourceMatcher(SubstringOrRegexPattern Pattern, EventLevel Level = EventLevel.LogAlways)
{
    internal bool IsMatch(EventSource eventSource) => Pattern.IsMatch(eventSource.Name);

    /// <summary>
    /// Implicitly convert a SubstringOrRegexPattern to an EventSourceMatcher
    /// </summary>
    public static implicit operator EventSourceMatcher(SubstringOrRegexPattern pattern) => new(pattern);

    /// <summary>
    /// Implicitly convert a string to an EventSourceMatcher
    /// </summary>
    public static implicit operator EventSourceMatcher(string pattern) => new(new SubstringOrRegexPattern(pattern));

    /// <summary>
    /// Implicitly convert a (SubstringOrRegexPattern, EventLevel) tuple to an EventSourceMatcher
    /// </summary>
    public static implicit operator EventSourceMatcher((SubstringOrRegexPattern pattern, EventLevel level) matcher)
        => new(matcher.pattern, matcher.level);

    /// <summary>
    /// Implicitly convert a (string, EventLevel) tuple to an EventSourceMatcher
    /// </summary>
    public static implicit operator EventSourceMatcher((string pattern, EventLevel level) matcher)
        => new(new SubstringOrRegexPattern(matcher.pattern), matcher.level);
}
#endif
