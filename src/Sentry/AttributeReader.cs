namespace Sentry;

internal static class AttributeReader
{
    public static string? TryGetProjectDirectory(Assembly assembly) =>
        assembly.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(x => x.Key == "Sentry.ProjectDirectory")?.Value;
    public static string? TryGetRepositoryRoot(Assembly assembly) =>
        assembly.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(x => x.Key == "Sentry.RepositoryRoot")?.Value;
}
