using Sentry.Internal;

namespace Sentry;

/// <summary>
/// Provides a pattern that can be used to match against other strings as either a substring or regular expression.
/// </summary>
/// <param name="comparison">The string comparison type to use when matching for substrings.</param>
public class SubstringOrPatternMatcher(StringComparison comparison = StringComparison.OrdinalIgnoreCase)
{
    internal bool IsMatch(StringOrRegex stringOrRegex, string value)
    {
        return (stringOrRegex._string != null && value.Contains(stringOrRegex._string, comparison)) ||
               stringOrRegex?._regex?.IsMatch(value) == true;
    }
}
