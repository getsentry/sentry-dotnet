using System;

namespace Sentry.Internal;

internal class PrefixOrPatternMatcher(StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    : IStringOrRegexMatcher
{
    public bool IsMatch(StringOrRegex stringOrRegex, string value)
    {
        return (stringOrRegex._prefix != null && value.StartsWith(stringOrRegex._prefix, comparison)) ||
               stringOrRegex?._regex?.IsMatch(value) == true;
    }
}
