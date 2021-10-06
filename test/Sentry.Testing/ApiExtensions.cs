using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using PublicApiGenerator;
using VerifyXunit;

namespace Sentry.Tests
{
    public static class ApiExtensions
    {
        public static Task CheckApproval(this Assembly assembly, [CallerFilePath] string filePath = "")
        {
            var generatorOptions = new ApiGeneratorOptions { WhitelistedNamespacePrefixes = new[] { "Sentry" } };
            var apiText = assembly.GeneratePublicApi(generatorOptions);
            return Verifier.Verify(apiText, null, filePath)
                .UniqueForRuntimeAndVersion()
                .ScrubEmptyLines()
                .ScrubLines(l =>
                    l.StartsWith("[assembly: AssemblyVersion(", StringComparison.InvariantCulture) ||
                    l.StartsWith("[assembly: AssemblyFileVersion(", StringComparison.InvariantCulture) ||
                    l.StartsWith("[assembly: AssemblyInformationalVersion(", StringComparison.InvariantCulture) ||
                    l.StartsWith("[assembly: System.Reflection.AssemblyMetadata(", StringComparison.InvariantCulture));
        }
    }
}
