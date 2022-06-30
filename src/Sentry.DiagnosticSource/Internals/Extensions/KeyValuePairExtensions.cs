using System.Collections.Generic;

namespace Sentry.Internal.Extensions
{
    internal static class KeyValuePairExtensions
    {
        public static T? GetProperty<T>(this KeyValuePair<string, object?> keyPair, string name)
        {
            if (keyPair.Value?.GetType()
                    .GetProperty(name)
                    ?.GetValue(keyPair.Value) is { } valueFound)
            {
                return (T)valueFound;
            }

            return default;
        }

        public static T? GetSubProperty<T>(this KeyValuePair<string, object?> keyPair, string propertyName, string subPropertyName)
        {
            if (keyPair.GetProperty<object>(propertyName) is { } value)
            {
                if (value.GetType()
                        .GetProperty(subPropertyName)
                        ?.GetValue(value) is { } valueFound)
                {
                    return (T)valueFound;
                }
            }

            return default;
        }
    }
}
