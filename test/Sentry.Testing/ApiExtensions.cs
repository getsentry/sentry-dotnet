#if !__MOBILE__
using System.Runtime.Versioning;
using PublicApiGenerator;

namespace Sentry.Testing;

public static class ApiExtensions
{
    public static Task CheckApproval(this Assembly assembly, [CallerFilePath] string filePath = "")
    {
        var generatorOptions = new ApiGeneratorOptions
        {
            ExcludeAttributes = new[]
            {
              typeof(AssemblyVersionAttribute).FullName,
              typeof(AssemblyFileVersionAttribute).FullName,
              typeof(AssemblyInformationalVersionAttribute).FullName,
              typeof(AssemblyMetadataAttribute).FullName,
              typeof(InternalsVisibleToAttribute).FullName,
              typeof(TargetFrameworkAttribute).FullName
            },
            AllowNamespacePrefixes = new[] { "Sentry", "Microsoft" }
        };
        var apiText = assembly.GeneratePublicApi(generatorOptions);

        // ReSharper disable once ExplicitCallerInfoArgument
        return Verify(apiText, null, filePath)
            .AutoVerify(includeBuildServer: false)
            .UniqueForTargetFrameworkAndVersion()
            .ScrubLinesContaining("AltCover.Recorder.Instrumentation")
            .ScrubEmptyLines();
    }
}
#endif
