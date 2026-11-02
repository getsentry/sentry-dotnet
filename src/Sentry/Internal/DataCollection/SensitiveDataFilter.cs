namespace Sentry.Internal.DataCollection;

/// <summary>
/// Filters automatically collected key-value data (HTTP headers, cookies, URL query parameters) so that values of
/// sensitive keys are replaced with "[Filtered]" before being sent to Sentry, per the DataCollection spec:
/// https://develop.sentry.dev/sdk/foundations/client/data-collection/
/// Key names are always preserved; only values are replaced. Only applies to automatically collected data —
/// data explicitly set by the user is never filtered.
/// </summary>
internal static class SensitiveDataFilter
{
    internal const string FilteredValue = PiiExtensions.RedactedText;

    /// <summary>
    /// The canonical sensitive denylist from the spec, matched as case-insensitive substrings against header names,
    /// cookie names and query parameter keys.
    /// </summary>
    internal static readonly string[] SensitiveKeyTerms =
    [
        "auth",
        "token",
        "secret",
        "password",
        "passwd",
        "pwd",
        "key",
        "jwt",
        "bearer",
        "sso",
        "saml",
        "csrf",
        "xsrf",
        "credentials",
        "session",
        "sid",
        "identity",
    ];

    /// <summary>
    /// Extra substrings matched only against individual cookie names, covering opaque session, sticky-routing and
    /// identity-provider cookies whose names contain no <see cref="SensitiveKeyTerms"/> match. Not applied to header
    /// names, where they would produce false positives.
    /// </summary>
    internal static readonly string[] SensitiveCookieNameTerms =
    [
        "remember",
        "oidc",
        "pkce",
        "nonce",
        // RFC 6265bis high-security cookie name prefixes
        "__secure-",
        "__host-",
        // Load balancer / CDN sticky-session cookies
        "awsalb",
        "awselb",
        "akamai",
        // Managed-service session cookies
        "__stripe",
        "cognito",
        "firebase",
        "supabase",
        "sb-",
        "mfa",
        "2fa",
    ];

    /// <summary>
    /// Key-name substrings that identify end users (IP addresses, forwarding chains). Not filtered by default;
    /// used to extend deny lists when replicating <c>SendDefaultPii = false</c> behavior (GDPR terms per the spec).
    /// </summary>
    internal static readonly string[] PiiKeyTerms = ["forwarded", "-ip", "remote-", "via", "-user"];

    internal static bool IsSensitiveKey(string key) => MatchesAny(key, SensitiveKeyTerms);

    /// <summary>
    /// Percent-decodes a key so an encoded name such as "%74oken" is matched against the denylist. Callers must
    /// still emit the original key and value: the spec requires encoded query parameters to be stored exactly as
    /// they appear in the URL.
    /// </summary>
    internal static string DecodeKeyForMatching(string key) => Uri.UnescapeDataString(key);

    /// <summary>
    /// Filters a single value according to <paramref name="behavior"/>. Returns the value unchanged, or
    /// <see cref="FilteredValue"/> in its place.
    /// </summary>
    internal static string FilterValue(string key, string value, KeyValueFilterBehavior behavior,
        string[]? additionalDenyTerms = null)
    {
        if (behavior.Mode == KeyValueFilterMode.Off ||
            MatchesAny(key, SensitiveKeyTerms) ||
            (additionalDenyTerms is not null && MatchesAny(key, additionalDenyTerms)))
        {
            return FilteredValue;
        }

        var matchesTerm = MatchesAny(key, behavior.Terms);
        if (behavior.Mode == KeyValueFilterMode.AllowList)
        {
            return matchesTerm ? value : FilteredValue;
        }

        return matchesTerm ? FilteredValue : value;
    }

    /// <summary>
    /// Filters key-value data according to <paramref name="behavior"/>. Key names are always preserved; values are
    /// either kept or replaced with <see cref="FilteredValue"/>. In <see cref="KeyValueFilterMode.Off"/> mode nothing
    /// is returned. <paramref name="additionalDenyTerms"/> are extra sensitive terms applied in every mode, beyond
    /// the built-in denylist (e.g. <see cref="SensitiveCookieNameTerms"/> when filtering cookies).
    /// Input order and repeated keys are preserved, because headers such as Set-Cookie may legitimately repeat.
    /// </summary>
    internal static List<KeyValuePair<string, string>> FilterKeyValueData(
        IEnumerable<KeyValuePair<string, string>> data,
        KeyValueFilterBehavior behavior,
        string[]? additionalDenyTerms = null)
    {
        var result = new List<KeyValuePair<string, string>>();
        if (behavior.Mode == KeyValueFilterMode.Off)
        {
            return result;
        }

        foreach (var (key, value) in data)
        {
            result.Add(new KeyValuePair<string, string>(key, FilterValue(key, value, behavior, additionalDenyTerms)));
        }

        return result;
    }

    private static bool MatchesAny(string key, string[] terms)
    {
        foreach (var term in terms)
        {
            if (!string.IsNullOrEmpty(term) && key.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
