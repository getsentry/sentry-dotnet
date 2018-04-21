using System;
using System.Diagnostics;
using System.Reflection;
#if HAS_RUNTIME_INFORMATION
using System.Runtime.InteropServices;
#endif

namespace Sentry.PlatformAbstractions
{
    public class RuntimeDetail
    {
        public static Runtime GetCurrentRuntime()
        {
            Runtime runtime = null;
#if HAS_RUNTIME_INFORMATION
            var versionString = GetFromRuntimeInformation();
#else
            runtime = GetFromMonoRuntime()
                      ?? GetFromEnvironmentVariable();
#endif
            return runtime;
        }

#if HAS_RUNTIME_INFORMATION
        internal static Runtime GetFromRuntimeInformation()
        {
            // Prefered API: netstandard2.0
            // https://github.com/dotnet/corefx/blob/master/src/System.Runtime.InteropServices.RuntimeInformation/src/System/Runtime/InteropServices/RuntimeInformation/RuntimeInformation.cs
            // https://github.com/mono/mono/blob/90b49aa3aebb594e0409341f9dca63b74f9df52e/mcs/class/corlib/System.Runtime.InteropServices.RuntimeInformation/RuntimeInformation.cs
            // e.g: .NET Framework 4.7.2633.0, .NET Native, WebAssembly
            var frameworkDescription = RuntimeInformation.FrameworkDescription;

            return Parse(frameworkDescription);
        }
#endif

        internal static Runtime GetFromMonoRuntime()
            => Type.GetType("Mono.Runtime", false)
#if HAS_TYPE_INFO
                ?.GetTypeInfo()
#endif
                ?.GetMethod("GetDisplayName", BindingFlags.NonPublic | BindingFlags.Static)
                ?.Invoke(null, null) is string monoVersion
                // The implementation of Mono to RuntimeInformation:
                // https://github.com/mono/mono/blob/90b49aa3aebb594e0409341f9dca63b74f9df52e/mcs/class/corlib/System.Runtime.InteropServices.RuntimeInformation/RuntimeInformation.cs#L40
                // e.g; Mono 5.10.1.47 (2017-12/8eb8f7d5e74 Fri Apr 13 20:18:12 EDT 2018)
                ? Parse(monoVersion, "Mono")
                : null;

        // This should really only .NET 1.0, 1.1, 2.0, 3.0, 3.5 and 4.0
        internal static Runtime GetFromEnvironmentVariable()
        {
            // Environment.Version: NET Framework 4, 4.5, 4.5.1, 4.5.2 = 4.0.30319.xxxxx
            // .NET Framework 4.6, 4.6.1, 4.6.2, 4.7, 4.7.1 =  4.0.30319.42000
            // Not recommended on NET45+ (which has RuntimeInformation)
            var version = Environment.Version;

            string friendlyVersion = null;
            switch (version.Major)
            {
                case 1:
                    friendlyVersion = "";
                    break;
                //case "4.0.30319.42000":
                //    Debug.Fail("This is .NET Framework 4.6 or later which support RuntimeInformation");
                //    break;
                default:
                    friendlyVersion = version.ToString();
                    break;

            }
            return new Runtime(".NET Framework", friendlyVersion, "Environment.Version=" + version);
        }

        internal static Runtime Parse(string frameworkDescription, string name = null)
        {
            if (frameworkDescription == null)
            {
                return null;
            }

            // assumes: Some Strings Some-Number possibly-more-info
            var firstNumber = name != null ? 0 : -1;
            int spaceAfterNumbers = -1;

            for (int i = 0; i < frameworkDescription.Length; i++)
            {
                if (firstNumber == -1 && char.IsNumber(frameworkDescription[i]))
                {
                    // version part started
                    firstNumber = i;
                }
                else if (firstNumber != -1 && char.IsWhiteSpace(frameworkDescription[i]))
                {
                    spaceAfterNumbers = i;
                    break;
                }
            }

            if (spaceAfterNumbers == -1)
            {
                spaceAfterNumbers = frameworkDescription.Length;
            }

            string version = null;
            if (firstNumber != -1)
            {
                name = name ?? frameworkDescription.Substring(0, firstNumber - 1);
                version = frameworkDescription.Substring(firstNumber, spaceAfterNumbers - name.Length - 1);
            }
            else
            {
                name = frameworkDescription;
            }

            return new Runtime(
                name,
                version,
                frameworkDescription);
        }
    }
}
