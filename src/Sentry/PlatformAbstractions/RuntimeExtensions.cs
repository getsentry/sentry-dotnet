using System;
using System.ComponentModel;

namespace Sentry.PlatformAbstractions
{
    /// <summary>
    /// Extension method to the <see cref="Runtime"/> class.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class RuntimeExtensions
    {
        /// <summary>
        /// Is the runtime instance .NET Framework.
        /// </summary>
        /// <param name="runtime">The runtime instance to check.</param>
        /// <returns>True if it's .NET Framework, otherwise false.</returns>
        public static bool IsNetFx(this Runtime runtime) => runtime.IsRuntime(".NET Framework");
        /// <summary>
        /// Is the runtime instance .NET Core.
        /// </summary>
        /// <param name="runtime">The runtime instance to check.</param>
        /// <returns>True if it's .NET Core, otherwise false.</returns>
        public static bool IsNetCore(this Runtime runtime) => runtime.IsRuntime(".NET Core");
        /// <summary>
        /// Is the runtime instance Mono.
        /// </summary>
        /// <param name="runtime">The runtime instance to check.</param>
        /// <returns>True if it's Mono, otherwise false.</returns>
        public static bool IsMono(this Runtime runtime) => runtime.IsRuntime("Mono");

        private static bool IsRuntime(this Runtime? runtime, string runtimeName)
        {
            return runtime?.Name?.StartsWith(runtimeName, StringComparison.OrdinalIgnoreCase) == true
                   || runtime?.Raw?.StartsWith(runtimeName, StringComparison.OrdinalIgnoreCase) == true;
        }
    }
}
