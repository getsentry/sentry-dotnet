namespace Sentry.Internal;

/// <summary>
/// Sanitizes data that potentially contains Personally Identifiable Information (PII) before sending it to Sentry.
/// </summary>
internal static class PiiUrlSanitizer
{
    /// <summary>
    /// Searches for URLs in text data and redacts any PII
    /// data from these, as required.
    /// </summary>
    /// <param name="data">The data to be searched</param>
    /// <returns>
    /// The data if SendDefaultPii is enabled or if the data does not contain any PII.
    /// A redacted copy of the data otherwise.
    /// </returns>
    public static string? Sanitize(this string? data)
    {
        // If the data is empty then we don't need to sanitize anything
        if (string.IsNullOrWhiteSpace(data))
        {
            return data;
        }

        // The pattern @"(?i)\b(https?://.*@.*)\b" uses the \b word boundary anchors to ensure that the match occurs at
        // a word boundary. This allows the URL to be matched even if it is part of a larger text. The (?i) flag ensures
        // case-insensitive matching for "https" or "http".
        var authRegex = new Regex(@"(?i)\b(https?://.*@.*)\b");
        var result = authRegex.Replace(data, match =>
        {
            var matchedUrl = match.Groups[1].Value;
            return SanitizeUrl(matchedUrl);
        });

        return result;
    }

    private static string SanitizeUrl(string data)
    {
        // ^ matches the start of the string. (?i)(https?://) gives a case-insensitive matching of the protocol.
        // (.*@) matches the username and password (authentication information). (.*)$ matches the rest of the URL.
        var userInfoMatcher = new Regex(@"^(?i)(https?://)(.*@)(.*)$").Match(data);
        if (userInfoMatcher.Success && userInfoMatcher.Groups.Count == 4)
        {
            var userInfoString = userInfoMatcher.Groups[2].Value;
            var replacementString = userInfoString.Contains(":") ? "[Filtered]:[Filtered]@" : "[Filtered]@";
            return userInfoMatcher.Groups[1].Value + replacementString + userInfoMatcher.Groups[3].Value;
        }
        else
        {
            return data;
        }
    }
}
