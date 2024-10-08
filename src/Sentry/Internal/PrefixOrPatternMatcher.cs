using System;

namespace Sentry.Internal;

internal class PrefixOrPatternMatcher(StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    : IStringOrRegexMatcher
{
    public bool IsMatch(StringOrRegex stringOrRegex, string value)
    {
        return (stringOrRegex._string != null && value.StartsWith(stringOrRegex._string, comparison)) ||
               stringOrRegex?._regex?.IsMatch(value) == true;
    }
}
