using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Sentry.AspNetCore.Extensions
{
    internal static class HeaderDictionaryExtensions
    {
        public static StringValues? GetValueOrDefault(this IHeaderDictionary headers, string key) =>
            headers.TryGetValue(key, out var value) ? (StringValues?)value : null;
    }
}
