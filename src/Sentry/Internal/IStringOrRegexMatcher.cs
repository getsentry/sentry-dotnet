namespace Sentry.Internal;

/// <summary>
/// Interface for class of comparers that can match against either a string or a regex.
/// </summary>
internal interface IStringOrRegexMatcher
{
    /// <summary>
    /// Evaluates if the given value matches the string or regex.
    /// </summary>
    bool IsMatch(StringOrRegex stringOrRegex, string value);
}
