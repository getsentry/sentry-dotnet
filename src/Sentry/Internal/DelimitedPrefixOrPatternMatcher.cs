using System;

namespace Sentry.Internal;

internal class DelimitedPrefixOrPatternMatcher(char delimiter = '.', StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    : IStringOrRegexMatcher
{
    public bool IsMatch(StringOrRegex stringOrRegex, string value)
    {
        if (stringOrRegex._string is not null)
        {
            // Check for a prefix followed by the separator
            return stringOrRegex._string != null && value.StartsWith(stringOrRegex._string, comparison) &&
                   value.Length > stringOrRegex._string.Length && value[stringOrRegex._string.Length] == delimiter;
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
