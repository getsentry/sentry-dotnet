using System.Linq;
using System.Reflection;

namespace Sentry;

internal static class AttributeReader
{
    public static bool TryGetProjectDirectory(Assembly assembly, out string? projectDirectory)
    {
        projectDirectory = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(x => x.Key == "Sentry.ProjectDirectory")
            ?.Value;
        return projectDirectory != null;
    }
}
