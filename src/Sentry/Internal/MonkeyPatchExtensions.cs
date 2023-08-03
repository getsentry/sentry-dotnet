using Sentry.Internal.Extensions;

namespace Sentry.Internal;

/// <summary>
/// Utility methods to enable storing arbitrary properties on any object, without worrying about garbage collection.
/// </summary>
internal static class MonkeyPatchExtensions
{
    private static ConditionalWeakTable<object, Dictionary<string, object?>> PropertyMap { get; } = new();

    private static Dictionary<string, object?> MonkeyProperties(this object source) =>
        PropertyMap.GetValue(source, _ => new Dictionary<string, object?>());

    internal static void Patch(this object source, string propertyName, object? value) =>
        source.MonkeyProperties()[propertyName] = value;

    internal static T? Patched<T>(this object source, string propertyName) =>
        source.MonkeyProperties().TryGetTypedValue<T>(propertyName, out var value)
            ? value
            : default;

    internal static T With<T>(this object source) where T : new()
    {
        var propertyName = typeof(T).Name;
        if (!source.MonkeyProperties().TryGetTypedValue<T>(propertyName, out var value))
        {
            value = new T();
            source.MonkeyProperties()[propertyName] = value;
        }
        return value;
    }
}
