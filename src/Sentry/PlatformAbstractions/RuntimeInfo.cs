using System;

namespace Sentry.PlatformAbstractions;

// https://github.com/dotnet/corefx/issues/17452
internal static class RuntimeInfo
{
    private static readonly Regex RuntimeParseRegex = new(
        @"^(?<name>(?:[A-Za-z.]\S*\s?)*)(?:\s|^|$)(?<version>\d\S*)?",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Gets the current runtime.
    /// </summary>
    /// <returns>A new instance for the current runtime</returns>
    internal static Runtime GetRuntime()
    {
        var runtime = GetFromRuntimeInformation();
        runtime ??= GetFromMonoRuntime();
        runtime ??= GetFromEnvironmentVariable();
        return runtime.WithAdditionalProperties();
    }

    internal static Runtime WithAdditionalProperties(this Runtime runtime)
    {
#if NETFRAMEWORK
            GetNetFxInstallationAndVersion(runtime, out var inst, out var ver);
            var version = runtime.Version ?? ver;
            var installation = runtime.FrameworkInstallation ?? inst;
            return new Runtime(runtime.Name, version, installation, runtime.Raw);
#elif NET5_0_OR_GREATER
            var version = runtime.Version ?? GetNetCoreVersion(runtime);
            var identifier = runtime.Identifier ?? GetRuntimeIdentifier(runtime);
            return new Runtime(runtime.Name, version, runtime.Raw, identifier);
#else
        var version = runtime.Version ?? GetNetCoreVersion(runtime);
        return new Runtime(runtime.Name, version, runtime.Raw);
#endif
    }

    internal static Runtime? Parse(string? rawRuntimeDescription, string? name = null)
    {
        if (rawRuntimeDescription == null)
        {
            return name == null ? null : new Runtime(name);
        }

        var match = RuntimeParseRegex.Match(rawRuntimeDescription);
        if (match.Success)
        {
            return new Runtime(
                name ?? (match.Groups["name"].Value == string.Empty ? null : match.Groups["name"].Value.Trim()),
                match.Groups["version"].Value == string.Empty ? null : match.Groups["version"].Value.Trim(),
                raw: rawRuntimeDescription);
        }

        return new Runtime(name, raw: rawRuntimeDescription);
    }

#if NETFRAMEWORK
        internal static void GetNetFxInstallationAndVersion(
            Runtime runtime,
            out FrameworkInstallation? frameworkInstallation,
            out string? version)
        {
            if (runtime.IsNetFx() != true)
            {
                frameworkInstallation = null;
                version = null;
                return;
            }

            frameworkInstallation = FrameworkInfo.GetLatest(Environment.Version.Major);

            if (frameworkInstallation?.Version?.Major < 4)
            {
                // prior to 4, user-friendly versions are always 2 digit: 1.0, 1.1, 2.0, 3.0, 3.5
                version = frameworkInstallation.ServicePack == null
                    ? $"{frameworkInstallation.Version.Major}.{frameworkInstallation.Version.Minor}"
                    : $"{frameworkInstallation.Version.Major}.{frameworkInstallation.Version.Minor} SP {frameworkInstallation.ServicePack}";
            }
            else
            {
                version = frameworkInstallation?.Version?.ToString();
            }
        }
#else
    private static string? GetNetCoreVersion(Runtime runtime)
    {
        var description = RuntimeInformation.FrameworkDescription;
        return RemovePrefixOrNull(description, ".NET Core")
           ?? RemovePrefixOrNull(description, ".NET Framework")
           ?? RemovePrefixOrNull(description, ".NET Native")
           ?? RemovePrefixOrNull(description, ".NET");

        static string? RemovePrefixOrNull(string? value, string prefix)
            => value?.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) == true
                ? value.Substring(prefix.Length)
                : null;
    }
#endif

#if NET5_0_OR_GREATER
        internal static string? GetRuntimeIdentifier(Runtime runtime)
        {
            try
            {
                return RuntimeInformation.RuntimeIdentifier;
            }
            catch
            {
                return null;
            }
        }
#endif

    private static Runtime? GetFromRuntimeInformation()
    {
        try
        {
            // Preferred API: netstandard2.0
            // https://github.com/dotnet/corefx/blob/master/src/System.Runtime.InteropServices.RuntimeInformation/src/System/Runtime/InteropServices/RuntimeInformation/RuntimeInformation.cs
            // https://github.com/mono/mono/blob/90b49aa3aebb594e0409341f9dca63b74f9df52e/mcs/class/corlib/System.Runtime.InteropServices.RuntimeInformation/RuntimeInformation.cs
            // e.g: .NET Framework 4.7.2633.0, .NET Native, WebAssembly
            // Note: this throws on some Unity IL2CPP versions
            var frameworkDescription = RuntimeInformation.FrameworkDescription;
            return Parse(frameworkDescription);
        }
        catch
        {
            return null;
        }
    }

    private static Runtime? GetFromMonoRuntime()
        => Type.GetType("Mono.Runtime", false)
            ?.GetMethod("GetDisplayName", BindingFlags.NonPublic | BindingFlags.Static)
            ?.Invoke(null, null) is string monoVersion
            // The implementation of Mono to RuntimeInformation:
            // https://github.com/mono/mono/blob/90b49aa3aebb594e0409341f9dca63b74f9df52e/mcs/class/corlib/System.Runtime.InteropServices.RuntimeInformation/RuntimeInformation.cs#L40
            // Examples:
            // Mono 5.10.1.47 (2017-12/8eb8f7d5e74 Fri Apr 13 20:18:12 EDT 2018)
            // Mono 5.10.1.47 (tarball Tue Apr 17 09:23:16 UTC 2018)
            // Mono 5.10.0 (Visual Studio built mono)
            ? Parse(monoVersion, "Mono")
            : null;

    // This should really only be used on .NET 1.0, 1.1, 2.0, 3.0, 3.5 and 4.0
    private static Runtime GetFromEnvironmentVariable()
    {
        // Environment.Version: NET Framework 4, 4.5, 4.5.1, 4.5.2 = 4.0.30319.xxxxx
        // .NET Framework 4.6, 4.6.1, 4.6.2, 4.7, 4.7.1 =  4.0.30319.42000
        // Not recommended on NET45+ (which has RuntimeInformation)
        var version = Environment.Version;

        var friendlyVersion = version.Major switch
        {
            1 => "",
            _ => version.ToString()
        };
        return new Runtime(".NET Framework", friendlyVersion, raw: "Environment.Version=" + version);
    }
}
