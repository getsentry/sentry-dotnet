namespace Sentry.Internal;

internal interface IStringOrRegexMatcher
{
    /// <summary>
    /// Evaluates if the given value matches the string or regex.
    /// </summary>
    public bool IsMatch(StringOrRegex stringOrRegex, string value);
}
