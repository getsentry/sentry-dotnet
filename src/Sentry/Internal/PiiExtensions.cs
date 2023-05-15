namespace Sentry.Internal;

/// <summary>
/// Extensions to help redact data that might contain Personally Identifiable Information (PII) before sending it to
/// Sentry.
/// </summary>
internal static class PiiExtensions
{
    internal const string RedactedText = "[Filtered]";

    /// <summary>
    /// Searches for URLs in text data and redacts any PII data from these, as required.
    /// </summary>
    /// <param name="data">The data to be searched</param>
    /// <returns>
    /// The data, if no PII data is present or a copy of the data with PII data redacted otherwise
    /// </returns>
    public static string? RedactUrl(this string? data)
    {
        // If the data is empty then we don't need to redact anything
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
            return RedactAuth(matchedUrl);
        });

        return result;
    }

    private static string RedactAuth(string data)
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
