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
        public static bool IsNetFx(this Runtime runtime) => runtime.StartsWith(".NET Framework");

        /// <summary>
        /// Is the runtime instance .NET Core (or .NET).
        /// </summary>
        /// <param name="runtime">The runtime instance to check.</param>
        /// <returns>True if it's .NET Core (or .NET), otherwise false.</returns>
        public static bool IsNetCore(this Runtime runtime) =>
            runtime.StartsWith(".NET Core") ||
            (runtime.StartsWith(".NET") && !runtime.StartsWith(".NET Framework"));

        /// <summary>
        /// Is the runtime instance Mono.
        /// </summary>
        /// <param name="runtime">The runtime instance to check.</param>
        /// <returns>True if it's Mono, otherwise false.</returns>
        public static bool IsMono(this Runtime runtime) => runtime.StartsWith("Mono");

        private static bool StartsWith(this Runtime? runtime, string runtimeName) =>
            runtime?.Name?.StartsWith(runtimeName, StringComparison.OrdinalIgnoreCase) == true ||
            runtime?.Raw?.StartsWith(runtimeName, StringComparison.OrdinalIgnoreCase) == true;
    }
}
