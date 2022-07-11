using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Sentry
{
    internal static class AttributeReader
    {
        public static bool TryGetProjectDirectory(Assembly assembly, out string? projectDirectory)
        {
            projectDirectory = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                .SingleOrDefault(x => x.Key == "Sentry.ProjectDirectory")
                ?.Value;
            return projectDirectory != null;
        }
    }
}
