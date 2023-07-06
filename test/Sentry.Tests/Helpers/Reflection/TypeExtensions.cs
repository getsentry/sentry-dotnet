namespace Sentry.Tests.Helpers.Reflection;

internal static class TypeExtensions
{
    public static void AssertImmutable(this Type type)
    {
        if (type.IsPrimitive ||
            type == typeof(string) ||
            type == typeof(DateTimeOffset) ||
            type == typeof(DateTime) ||
            type.IsEnum ||
            type.Name.StartsWith("IImmutable"))
        {
            return;
        }

        var fieldInfos = type.GetFields(BindingFlags.Public
                                        | BindingFlags.NonPublic
                                        | BindingFlags.Instance);

        var notReadOnly = fieldInfos.Where(f => !f.IsInitOnly).ToList();
        if (notReadOnly.Any())
        {
            throw new Exception($"Type {type} has non readonly fields: " +
                                string.Join(", ", notReadOnly.Select(p => p.Name)));
        }

        foreach (var fieldInfo in fieldInfos)
        {
            AssertImmutable(fieldInfo.FieldType);
        }
    }
}
