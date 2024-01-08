namespace Sentry.PlatformAbstractions;

/// <summary>
/// Extension method to the <see cref="SentryRuntime"/> class.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class RuntimeExtensions
{
    /// <summary>
    /// Is the runtime instance .NET Framework.
    /// </summary>
    /// <param name="runtime">The runtime instance to check.</param>
    /// <returns>True if it's .NET Framework, otherwise false.</returns>
    public static bool IsNetFx(this SentryRuntime runtime) => runtime.StartsWith(".NET Framework");

    /// <summary>
    /// Is the runtime instance .NET Core (or .NET).
    /// </summary>
    /// <param name="runtime">The runtime instance to check.</param>
    /// <returns>True if it's .NET Core (or .NET), otherwise false.</returns>
    public static bool IsNetCore(this SentryRuntime runtime) =>
        runtime.StartsWith(".NET Core") ||
        (runtime.StartsWith(".NET") && !runtime.StartsWith(".NET Framework"));

    /// <summary>
    /// Is the runtime instance Mono.
    /// </summary>
    /// <param name="runtime">The runtime instance to check.</param>
    /// <returns>True if it's Mono, otherwise false.</returns>
    public static bool IsMono(this SentryRuntime runtime) => runtime.StartsWith("Mono");

    /// <summary>
    /// Is the runtime instance Browser Web Assembly.
    /// </summary>
    /// <param name="runtime">The runtime instance to check.</param>
    /// <returns>True if it's Browser WASM, otherwise false.</returns>
    internal static bool IsBrowserWasm(this SentryRuntime runtime) => runtime.Identifier == "browser-wasm";

    private static bool StartsWith(this SentryRuntime? runtime, string runtimeName) =>
        runtime?.Name?.StartsWith(runtimeName, StringComparison.OrdinalIgnoreCase) == true ||
        runtime?.Raw?.StartsWith(runtimeName, StringComparison.OrdinalIgnoreCase) == true;
}
