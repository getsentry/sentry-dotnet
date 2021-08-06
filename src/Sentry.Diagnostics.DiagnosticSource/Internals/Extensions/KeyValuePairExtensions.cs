using System.Collections.Generic;

namespace Sentry.Internal.Extensions
{
    internal static class KeyValuePairExtensions
    {
        public static T? GetProperty<T>(this KeyValuePair<string, object?> keyPair, string name)
            => keyPair.Value?.GetType()
                .GetProperty(name)
                ?.GetValue(keyPair.Value) is { } valueFound ? (T)valueFound : default;

        public static T? GetSubProperty<T>(this KeyValuePair<string, object?> keyPair, string propertyName, string subPropertyName)
            => keyPair.GetProperty<object>(propertyName) is { } value ?
                value.GetType()
                .GetProperty(subPropertyName)
                ?.GetValue(value) is { } valueFound ? (T)valueFound : default
            : default;
    }
}
