using Sentry.Internal.Extensions;

namespace Sentry.Internal;

/// <summary>
/// Copied/Modified from
/// https://github.com/mentaldesk/fuse/blob/91af00dc9bc7e1deb2f11ab679c536194f85dd4a/MentalDesk.Fuse/ObjectExtensions.cs
/// </summary>
internal static class ObjectExtensions
{
    private static ConditionalWeakTable<object, Dictionary<string, object?>> Map { get; } = new();

    private static Dictionary<string, object?> AssociatedProperties(this object source) =>
        Map.GetValue(source, _ => new Dictionary<string, object?>());

    public static void SetFused(this object source, string propertyName, object? value) =>
        source.AssociatedProperties()[propertyName] = value;

    public static void SetFused<T>(this object source, T value) => SetFused(source, typeof(T).Name, value);

    public static T? GetFused<T>(this object source, string? propertyName = null)
    {
        propertyName ??= typeof(T).Name;
        return source.AssociatedProperties().TryGetTypedValue<T>(propertyName, out var value)
            ? value
            : default;
    }
}
