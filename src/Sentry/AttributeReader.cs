using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Sentry
{
    internal static class AttributeReader
    {
        public static bool TryGetProjectDirectory(Assembly assembly, out string? projectDirectory)
        {
            projectDirectory = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(x => x.Key == "Sentry.ProjectDirectory")
                ?.Value;

            // On Windows, ensure that we have "C:\" rather than "c:\" so that later when we replace
            // we don't have to worry about case sensitivity.
            if (projectDirectory != null && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var root = Path.GetPathRoot(projectDirectory);
                if (root?.Contains(Path.VolumeSeparatorChar) is true)
                {
#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                    projectDirectory = root.ToUpperInvariant() + projectDirectory[root.Length..];
#else
                    projectDirectory = root.ToUpperInvariant() + projectDirectory.Substring(root.Length);
#endif
                }
            }

            return projectDirectory != null;
        }
    }
}
