namespace Sentry.Internal.DataCollection;

internal static class CookieFilter
{
    internal const string RequestCookieAttribute = "http.request.header.cookie";

    private const string EventCookieSeparator = "; ";

    internal static string[] FilterCookieHeader(string? header, KeyValueCollectionBehavior? behavior = null)
    {
        behavior ??= KeyValueCollectionBehavior.DenyList();
        if (behavior.Mode == KeyValueCollectionMode.Off || string.IsNullOrWhiteSpace(header))
        {
            return [];
        }

        var filtered = new List<string>();
        foreach (var rawSegment in header.Split(';'))
        {
            var segment = rawSegment.Trim();
            if (segment.Length == 0)
            {
                continue;
            }

            var separator = segment.IndexOf('=');
            var name = separator < 0 ? null : segment[..separator].TrimEnd();
            var value = separator < 0 ? segment : segment[(separator + 1)..].TrimStart();
            if (FilterPair(name, value, behavior) is not { } cookie)
            {
                return [SensitiveDataFilter.FilteredValue];
            }

            filtered.Add(cookie);
        }

        return filtered.Count > 0 ? filtered.ToArray() : [SensitiveDataFilter.FilteredValue];
    }

    internal static string[] FilterCookieHeaders(IEnumerable<string?> headers, KeyValueCollectionBehavior? behavior = null) =>
        headers.SelectMany(header => FilterCookieHeader(header, behavior)).ToArray();

    internal static string[] FilterSetCookieHeader(string? header, KeyValueCollectionBehavior? behavior = null)
    {
        behavior ??= KeyValueCollectionBehavior.DenyList();
        if (behavior.Mode == KeyValueCollectionMode.Off || string.IsNullOrWhiteSpace(header))
        {
            return [];
        }

        var attributesStart = header.IndexOf(';');
        var cookie = (attributesStart < 0 ? header : header[..attributesStart]).Trim();
        var separator = cookie.IndexOf('=');
        var name = separator < 0 ? null : cookie[..separator].TrimEnd();
        var value = separator < 0 ? cookie : cookie[(separator + 1)..].TrimStart();
        return [FilterPair(name, value, behavior) ?? SensitiveDataFilter.FilteredValue];
    }

    internal static string[] FilterCookiePairs(IEnumerable<KeyValuePair<string?, string?>> cookies, KeyValueCollectionBehavior? behavior = null)
    {
        behavior ??= KeyValueCollectionBehavior.DenyList();
        if (behavior.Mode == KeyValueCollectionMode.Off)
        {
            return [];
        }

        var filtered = new List<string>();
        foreach (var (name, value) in cookies)
        {
            if (FilterPair(name, value ?? string.Empty, behavior) is not { } cookie)
            {
                return [SensitiveDataFilter.FilteredValue];
            }

            filtered.Add(cookie);
        }

        return filtered.ToArray();
    }

    internal static string ToEventCookies(IEnumerable<string> filteredCookies) =>
        string.Join(EventCookieSeparator, filteredCookies);

    private static string? FilterPair(string? name, string value, KeyValueCollectionBehavior behavior)
    {
        if (string.IsNullOrEmpty(name))
        {
            return SensitiveDataFilter.FilteredValue;
        }

        if (!IsValidName(name) || !IsValidValue(value))
        {
            return null;
        }

        return $"{name}={SensitiveDataFilter.FilterValue(name, value, behavior, SensitiveDataFilter.SensitiveCookieNameTerms)}";
    }

    private static bool IsValidName(string name)
    {
        foreach (var c in name)
        {
            if (!IsTokenCharacter(c))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsTokenCharacter(char c) =>
        c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9'
            or '!' or '#' or '$' or '%' or '&' or '\'' or '*' or '+' or '-' or '.' or '^' or '_' or '`' or '|' or '~';

    // A comma or space means the segment may hold more than one cookie, e.g. "theme=dark, session=abc"
    private static bool IsValidValue(string value)
    {
        var start = 0;
        var end = value.Length;
        if (value.Length > 0 && value[0] == '"')
        {
            if (value.Length < 2 || value[^1] != '"')
            {
                return false;
            }

            start++;
            end--;
        }

        for (var i = start; i < end; i++)
        {
            if (!IsCookieOctet(value[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsCookieOctet(char c) =>
        c is '\x21' or >= '\x23' and <= '\x2B' or >= '\x2D' and <= '\x3A' or >= '\x3C' and <= '\x5B' or >= '\x5D' and <= '\x7E';
}
