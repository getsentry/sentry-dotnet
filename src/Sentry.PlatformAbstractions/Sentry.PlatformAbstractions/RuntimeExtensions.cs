using System.ComponentModel;

namespace Sentry.PlatformAbstractions
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class RuntimeExtensions
    {
        public static bool IsNetFx(this Runtime runtime) => runtime.IsRuntime(".NET Framework");
        public static bool IsNetCore(this Runtime runtime) => runtime.IsRuntime(".NET Core");
        public static bool IsMono(this Runtime runtime) => runtime.IsRuntime("Mono");

        private static bool IsRuntime(this Runtime runtime, string runtimeName)
        {
            return runtime?.Name?.StartsWith(runtimeName) == true
                   || runtime?.Raw?.StartsWith(runtimeName) == true;
        }
    }
}
