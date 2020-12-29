using System;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

namespace Sentry.PlatformAbstractions
{
    // https://github.com/dotnet/corefx/issues/17452
    internal static class RuntimeInfo
    {
        private static readonly Regex RuntimeParseRegex = new("^(?<name>[^\\d]*)(?<version>(\\d+\\.)+[^\\s]+)",
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

#if NETFX
            SetNetFxReleaseAndVersion(runtime);
#elif NETSTANDARD || NETCOREAPP // Possibly .NET Core
            SetNetCoreVersion(runtime);
#endif
            return runtime;
        }

        internal static Runtime? Parse(string rawRuntimeDescription, string? name = null)
        {
            if (rawRuntimeDescription == null)
            {
                return name == null
                    ? null
                    : new Runtime(name);
            }

            var match = RuntimeParseRegex.Match(rawRuntimeDescription);
            if (match.Success)
            {
                return new Runtime(
                    name ?? (match.Groups["name"].Value == string.Empty ? null : match.Groups["name"].Value.Trim()),
                    match.Groups["version"].Value == string.Empty ? null : match.Groups["version"].Value.Trim(),
                    raw: rawRuntimeDescription
                );
            }

            return new Runtime(name, raw: rawRuntimeDescription);
        }

#if NETFX
        internal static void SetNetFxReleaseAndVersion(Runtime runtime)
        {
            if (runtime?.IsNetFx() == true)
            {
                var latest = FrameworkInfo.GetLatest(Environment.Version.Major);

                runtime.FrameworkInstallation = latest;
                if (latest.Version?.Major < 4)
                {
                    // prior to 4, user-friendly versions are always 2 digit: 1.0, 1.1, 2.0, 3.0, 3.5
                    runtime.Version = latest.ServicePack == null
                        ? $"{latest.Version.Major}.{latest.Version.Minor}"
                        : $"{latest.Version.Major}.{latest.Version.Minor} SP {latest.ServicePack}";
                }
                else
                {
                    runtime.Version = latest.Version?.ToString();
                }
            }
        }
#endif

#if NETSTANDARD || NETCOREAPP // Possibly .NET Core
        // Known issue on Docker: https://github.com/dotnet/BenchmarkDotNet/issues/448#issuecomment-361027977
        internal static void SetNetCoreVersion(Runtime runtime)
        {
            if (runtime.IsNetCore())
            {
                // https://github.com/dotnet/BenchmarkDotNet/issues/448#issuecomment-308424100
                var assembly = typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly;
#if NET5_0 || NETCOREAPP3_0
                var assemblyPath = assembly.Location.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
#else
                var assemblyPath = assembly.CodeBase.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
#endif
                var netCoreAppIndex = Array.IndexOf(assemblyPath, "Microsoft.NETCore.App");
                if (netCoreAppIndex > 0
                    && netCoreAppIndex < assemblyPath.Length - 2)
                {
                    runtime.Version = assemblyPath[netCoreAppIndex + 1];
                }
            }
        }
#endif

        internal static Runtime? GetFromRuntimeInformation()
        {
            // Prefered API: netstandard2.0
            // https://github.com/dotnet/corefx/blob/master/src/System.Runtime.InteropServices.RuntimeInformation/src/System/Runtime/InteropServices/RuntimeInformation/RuntimeInformation.cs
            // https://github.com/mono/mono/blob/90b49aa3aebb594e0409341f9dca63b74f9df52e/mcs/class/corlib/System.Runtime.InteropServices.RuntimeInformation/RuntimeInformation.cs
            // e.g: .NET Framework 4.7.2633.0, .NET Native, WebAssembly
            var frameworkDescription = RuntimeInformation.FrameworkDescription;

            return Parse(frameworkDescription);
        }

        internal static Runtime? GetFromMonoRuntime()
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
        internal static Runtime GetFromEnvironmentVariable()
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
}
