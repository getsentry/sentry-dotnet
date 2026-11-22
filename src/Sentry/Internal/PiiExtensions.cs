namespace Sentry.Internal;

/// <summary>
/// Extensions to help redact data that might contain Personally Identifiable Information (PII) before sending it to
/// Sentry.
/// </summary>
internal static class PiiExtensions
{
    internal const string RedactedText = "[Filtered]";

    // Userinfo ends at the last '@' before the path, query or fragment, as sentry-python parses it
    private static readonly Regex UserInfoRegex = new(@"(?i)\b(https?://)([^\s/?#]*)@", RegexOptions.Compiled);

    /// <summary>
    /// Replaces the userinfo (credentials) of any http or https URLs in <paramref name="data"/> with a placeholder.
    /// </summary>
    public static string RedactUrl(this string data)
    {
        if (string.IsNullOrWhiteSpace(data) || data.IndexOf('@') < 0)
        {
            return data;
        }

        return UserInfoRegex.Replace(data, match =>
            match.Groups[1].Value + (match.Groups[2].Value.Contains(":") ? "[Filtered]:[Filtered]@" : "[Filtered]@"));
    }
}
