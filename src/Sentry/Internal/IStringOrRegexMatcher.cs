namespace Sentry.Internal;

internal interface IStringOrRegexMatcher
{
    bool IsMatch(StringOrRegex stringOrRegex, string value);
}
