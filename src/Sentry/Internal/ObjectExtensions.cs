using Sentry.Internal.Extensions;

namespace Sentry.Internal;

/// <summary>
/// Utility methods to enable storing arbitrary properties on any object, without worrying about garbage collection.
/// </summary>
internal static class ObjectExtensions
{
    private static ConditionalWeakTable<object, Dictionary<string, object?>> Map { get; } = new();

    private static Dictionary<string, object?> AssociatedProperties(this object source) =>
        Map.GetValue(source, _ => new Dictionary<string, object?>());

    internal static void SetFused(this object source, string propertyName, object? value) =>
        AssociatedProperties(source)[propertyName] = value;

    internal static void SetFused<T>(this object source, T value)
    {
        var propertyName = typeof(T).Name;
        source.AssociatedProperties()[propertyName] = value;
    }

    internal static T? GetFused<T>(this object source, string? propertyName = null)
    {
        propertyName ??= typeof(T).Name;
        return AssociatedProperties(source).TryGetTypedValue<T>(propertyName, out var value)
            ? value
            : default;
    }
    internal static T Fused<T>(this object source) where T : new()
    {
        var propertyName = typeof(T).Name;
        if (!source.AssociatedProperties().TryGetTypedValue<T>(propertyName, out var value))
        {
            value = new T();
            source.AssociatedProperties()[propertyName] = value;
        }
        return value;
    }
}
