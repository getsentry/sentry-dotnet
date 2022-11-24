#if !__MOBILE__
using PublicApiGenerator;

namespace Sentry.Testing;

public static class ApiExtensions
{
    public static Task CheckApproval(this Assembly assembly, [CallerFilePath] string filePath = "")
    {
        var generatorOptions = new ApiGeneratorOptions
        {
            WhitelistedNamespacePrefixes = new[] { "Sentry", "Microsoft" }
        };
        var apiText = assembly.GeneratePublicApi(generatorOptions);
        return Verify(apiText, null, filePath)
            .AutoVerify(includeBuildServer: false)
            .UniqueForTargetFrameworkAndVersion()
            .ScrubEmptyLines()
            .ScrubLines(l =>
                l.StartsWith("[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(") ||
                l.StartsWith("[assembly: AssemblyVersion(") ||
                l.StartsWith("[assembly: System.Runtime.Versioning.TargetFramework(") ||
                l.StartsWith("[assembly: AssemblyFileVersion(") ||
                l.StartsWith("[assembly: AssemblyInformationalVersion(") ||
                l.StartsWith("[assembly: System.Reflection.AssemblyMetadata("));
    }
}
#endif
