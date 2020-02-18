using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using Sentry.Protocol;

namespace Sentry.Reflection
{
    /// <summary>
    /// Extension methods to <see cref="Assembly"/>
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class AssemblyExtensions
    {
        /// <summary>
        /// Get the assemblies Name and Version
        /// </summary>
        /// <remarks>
        /// Attempts to read the version from <see cref="AssemblyInformationalVersionAttribute"/>
        /// If not available, falls back to <see cref="AssemblyName.Version"/>
        /// </remarks>
        /// <param name="asm">The assembly to get the name and version from</param>
        /// <returns>The SdkVersion.</returns>
        public static SdkVersion GetNameAndVersion(this Assembly asm)
        {
            var asmName = asm.GetName();
            var name = asmName.Name;
            var version = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                          ?.InformationalVersion
                      ?? asmName.Version.ToString();

            return new SdkVersion { Name = name, Version = version };
        }

        /// <summary>
        /// Whether the assembly was compiled with the optimize+ flag
        /// </summary>
        /// <param name="asm">The assembly to verify the optimization flag</param>
        /// <returns>
        /// true if no <see cref="DebuggableAttribute"/> exists or
        /// <see cref="DebuggableAttribute.IsJITOptimizerDisabled"/> is false,
        /// otherwise, false.
        /// </returns>
        public static bool IsOptimized(this Assembly asm)
        {
            var att = asm.GetCustomAttribute<DebuggableAttribute>();
            return att == null || att.IsJITOptimizerDisabled == false;
        }
    }
}
