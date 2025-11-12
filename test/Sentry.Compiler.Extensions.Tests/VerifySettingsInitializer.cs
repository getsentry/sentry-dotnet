namespace Sentry.Compiler.Extensions.Tests;

internal static class VerifySettingsInitializer
{
    private static readonly AssemblyName s_assemblyName = typeof(BuildPropertySourceGenerator).Assembly.GetName();

    [ModuleInitializer]
    internal static void Initialize()
    {
        VerifierSettings.AddScrubber(VersionScrubber);
    }

    private static void VersionScrubber(StringBuilder text)
    {
        if (s_assemblyName.Version is not null)
        {
            text.Replace(s_assemblyName.Version.ToString(), nameof(Version));
        }
    }
}
