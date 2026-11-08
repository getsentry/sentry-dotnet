namespace Sentry.Internal.DataCollection;

internal static class QueryStringFilter
{
    private static readonly char[] QueryOrFragmentStart = ['?', '#'];

    // Works on the raw string because the spec requires encoded parameters to be stored exactly as they appear in the URL.
    internal static string? Filter(string? queryString)
    {
        if (string.IsNullOrEmpty(queryString))
        {
            return queryString;
        }

        var prefix = queryString[0] == '?' ? "?" : string.Empty;
        var parameters = queryString[prefix.Length..].Split('&');
        for (var i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            var separator = parameter.IndexOf('=');
            var name = separator < 0 ? parameter : parameter[..separator];
            if (HasMalformedEscape(name) || (name.Length == 0 && parameter.Length > 1))
            {
                return SensitiveDataFilter.FilteredValue;
            }

            if (separator >= 0 && SensitiveDataFilter.IsSensitiveKey(SensitiveDataFilter.DecodeKeyForMatching(name)))
            {
                parameters[i] = $"{name}={SensitiveDataFilter.FilteredValue}";
            }
        }

        return prefix + string.Join("&", parameters);
    }

    internal static string FilterUrl(string url)
    {
        var queryStart = url.IndexOf('?');
        var fragmentStart = url.IndexOf('#');
        if (queryStart < 0 || (fragmentStart >= 0 && fragmentStart < queryStart))
        {
            return url;
        }

        var queryEnd = fragmentStart < 0 ? url.Length : fragmentStart;
        return $"{url[..(queryStart + 1)]}{Filter(url[(queryStart + 1)..queryEnd])}{url[queryEnd..]}";
    }

    internal static string RemoveQueryAndFragment(string url)
    {
        var end = url.IndexOfAny(QueryOrFragmentStart);
        return end < 0 ? url : url[..end];
    }

    private static bool HasMalformedEscape(string name)
    {
        for (var i = name.IndexOf('%'); i >= 0; i = name.IndexOf('%', i + 1))
        {
            if (i + 2 >= name.Length || !Uri.IsHexDigit(name[i + 1]) || !Uri.IsHexDigit(name[i + 2]))
            {
                return true;
            }
        }

        return false;
    }
}
