namespace Sentry;

internal static class AttributeReader
{
    public static string? TryGetProjectDirectory(Assembly assembly)
    {
        // Use metadata (CustomAttributeData) to avoid hard-referencing AssemblyMetadataAttribute,
        // which may be trimmed. This safely returns null if the attribute instances were removed.
        // The constructor is: `AssemblyMetadataAttribute(string key, string? value);`
        foreach (var cad in assembly.GetCustomAttributesData())
        {
            var at = cad.AttributeType;
            if (at is { Namespace: "System.Reflection", Name: "AssemblyMetadataAttribute" }
                && cad.ConstructorArguments.Count == 2
                && cad.ConstructorArguments[0].ArgumentType == typeof(string)
                && string.Equals(cad.ConstructorArguments[0].Value as string, "Sentry.ProjectDirectory", StringComparison.Ordinal)
                && cad.ConstructorArguments[1].ArgumentType == typeof(string)
                )
            {
                return cad.ConstructorArguments[1].Value as string;
            }
        }

        return null;
    }
}
