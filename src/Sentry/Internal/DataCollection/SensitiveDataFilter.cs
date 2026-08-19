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
    /// Case-insensitive substrings identifying sensitive key names, from the canonical denylist in the spec,
    /// plus "set-cookie"/"cookie" so cookie headers are treated as sensitive wherever individual cookie
    /// pairs cannot be extracted.
    /// </summary>
    internal static readonly string[] SensitiveKeyTerms =
    [
        "auth",
        "token",
        "secret",
        "session",
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
        "sid",
        "identity",
        "set-cookie",
        "cookie",
    ];

    /// <summary>
    /// Extra substrings matched only against individual Cookie / Set-Cookie names (not header names), covering
    /// common session secrets that do not match <see cref="SensitiveKeyTerms"/> (e.g. "connect.sid") without
    /// false positives on arbitrary HTTP headers. Cookie-only terms already implied by a
    /// <see cref="SensitiveKeyTerms"/> match (e.g. "oauth", "id_token") are omitted.
    /// </summary>
    internal static readonly string[] SensitiveCookieNameTerms =
    [
        // Express / Connect default session cookie
        ".sid",
        // Opaque session ids (PHPSESSID, ASPSESSIONID*, *sessid*, ...)
        "sessid",
        // "Remember me" tokens
        "remember",
        // OIDC / OAuth auxiliary ("oauth*" is covered by "auth")
        "oidc",
        "pkce",
        "nonce",
        // RFC 6265bis high-security cookie name prefixes
        "__secure-",
        "__host-",
        // Load balancer / CDN sticky-session cookies (opaque routing tokens)
        "awsalb",
        "awselb",
        "akamai",
        // BaaS / IdP session cookies (names often omit "session")
        "__stripe",
        "cognito",
        "firebase",
        "supabase",
        "sb-",
        // Step-up / MFA cookies
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
    /// Filters a single value according to <paramref name="behavior"/>. Returns the value unchanged, or
    /// <see cref="FilteredValue"/> in its place. Must not be called in <see cref="KeyValueFilterMode.Off"/> mode
    /// (in off mode nothing should be collected at all).
    /// </summary>
    internal static string FilterValue(string key, string value, KeyValueFilterBehavior behavior,
        string[]? additionalDenyTerms = null)
    {
        Debug.Assert(behavior.Mode != KeyValueFilterMode.Off,
            "FilterValue must not be called in Off mode - collect nothing instead.");

        if (MatchesAny(key, SensitiveKeyTerms) ||
            (additionalDenyTerms is not null && MatchesAny(key, additionalDenyTerms)))
        {
            return FilteredValue;
        }

        return behavior.Mode switch
        {
            KeyValueFilterMode.DenyList => MatchesAny(key, behavior.Terms) ? FilteredValue : value,
            KeyValueFilterMode.AllowList => MatchesAny(key, behavior.Terms) ? value : FilteredValue,
            _ => value
        };
    }

    /// <summary>
    /// Filters key-value data according to <paramref name="behavior"/>. Key names are always preserved; values are
    /// either kept or replaced with <see cref="FilteredValue"/>. In <see cref="KeyValueFilterMode.Off"/> mode an
    /// empty dictionary is returned. <paramref name="additionalDenyTerms"/> are extra sensitive terms applied in
    /// every mode, beyond the built-in denylist (e.g. <see cref="SensitiveCookieNameTerms"/> when filtering cookies).
    /// </summary>
    internal static Dictionary<string, string> FilterKeyValueData(
        IEnumerable<KeyValuePair<string, string>> data,
        KeyValueFilterBehavior behavior,
        string[]? additionalDenyTerms = null)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (behavior.Mode == KeyValueFilterMode.Off)
        {
            return result;
        }

        foreach (var (key, value) in data)
        {
            result[key] = FilterValue(key, value, behavior, additionalDenyTerms);
        }

        return result;
    }

    private static bool MatchesAny(string key, string[] terms)
    {
        foreach (var term in terms)
        {
            if (key.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
