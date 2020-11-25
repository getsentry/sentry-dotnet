using System;

namespace Sentry.Internal.Extensions
{
    internal static class EnumExtensions
    {
        public static T ParseEnum<T>(this string str) where T : struct, Enum
        {
            return (T)Enum.Parse(typeof(T), str, true);
        }
    }
}
