using System.Text.RegularExpressions;

namespace Sentry.Internal.Extensions
{
    internal static class StringExtensions
    {
        // Used to convert enum value into snake case, which is how Sentry represents them
        public static string ToSnakeCase(this string str) =>
            Regex.Replace(str, @"(\p{Ll})(\p{Lu})", "$1_$2").ToLowerInvariant();
    }
}
