using System.Text.RegularExpressions;

namespace Sentry.Internal.Extensions
{
    internal static class StringExtensions
    {
        public static string ToSnakeCase(this string str) =>
            Regex.Replace(str, @"(\p{Ll})(\p{Lu})", "$1_$2").ToLowerInvariant();
    }
}
