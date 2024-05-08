using System;

namespace Sentry.Internal;

internal class DelimitedPrefixOrPatternMatcher(char delimiter = '.', StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    : IStringOrRegexMatcher
{
    public bool IsMatch(StringOrRegex stringOrRegex, string value)
    {
        if (stringOrRegex._prefix is not null)
        {
            // Check for a prefix followed by the separator
            return stringOrRegex._prefix != null && value.StartsWith(stringOrRegex._prefix, comparison) &&
                   value.Length > stringOrRegex._prefix.Length && value[stringOrRegex._prefix.Length] == delimiter;
        }

        // Check for any regex match followed by the separator
        if (stringOrRegex._regex is not null)
        {
            foreach (Match match in stringOrRegex._regex.Matches(value))
            {
                if (value.Length > match.Value.Length && value[match.Value.Length] == delimiter)
                {
                    return true;
                }
            }
        }
        return false;
    }
}
