using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Sentry
{
    internal static class AttributeReader
    {
        public static bool TryGetProjectDirectory([NotNullWhen(true)] out string? projectDirectory) =>
            TryGetProjectDirectory(Assembly.GetCallingAssembly(), out projectDirectory);

        public static bool TryGetProjectDirectory(Assembly assembly, [NotNullWhen(true)] out string? projectDirectory) =>
            TryGetValue(assembly, "Sentry.ProjectDirectory", out projectDirectory);

        public static bool TryGetSolutionDirectory([NotNullWhen(true)] out string? solutionDirectory) =>
            TryGetSolutionDirectory(Assembly.GetCallingAssembly(), out solutionDirectory);

        public static bool TryGetSolutionDirectory(Assembly assembly, [NotNullWhen(true)] out string? solutionDirectory) =>
            TryGetValue(assembly, "Sentry.SolutionDirectory", out solutionDirectory);

        private static bool TryGetValue(Assembly assembly, string key, [NotNullWhen(true)] out string? value)
        {
            value = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                .SingleOrDefault(x => x.Key == key)
                ?.Value;
            return value != null;
        }
    }
}
