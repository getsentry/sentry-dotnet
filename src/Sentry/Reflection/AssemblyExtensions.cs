using System.ComponentModel;
using System.Reflection;

namespace Sentry.Reflection;

/// <summary>
/// Extension methods to <see cref="Assembly"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
[Obsolete("Should not be public. This method will be removed in version 4.")]
public static class AssemblyExtensions
{
    /// <summary>
    /// Get the assemblies Name and Version.
    /// </summary>
    /// <remarks>
    /// Attempts to read the version from <see cref="AssemblyInformationalVersionAttribute"/>.
    /// If not available, falls back to <see cref="AssemblyName.Version"/>.
    /// </remarks>
    /// <param name="asm">The assembly to get the name and version from.</param>
    /// <returns>The SdkVersion.</returns>
    public static SdkVersion GetNameAndVersion(this Assembly asm)
    {
        return new SdkVersion
        {
            Name = asm.GetName().Name,
            Version = asm.GetVersion()
        };
    }

    internal static string? GetVersion(this Assembly assembly)
    {
        try
        {
            var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (informationalVersion != null)
            {
                return informationalVersion;
            }
        }
        catch
        {
            // Note: on full .NET FX, checking the AssemblyInformationalVersionAttribute could throw an exception,
            // therefore this method uses a try/catch to make sure this method always returns a value
        }

        // Note: even though the informational version could be "invalid" (e.g. string.Empty), it should
        // be used for versioning and the software should not fallback to the assembly version string.
        // See https://github.com/getsentry/sentry-dotnet/pull/1079#issuecomment-866992216
        // TODO: Lets change this in a new major to return the Version as fallback
        return assembly.GetName().Version?.ToString();
    }
}
