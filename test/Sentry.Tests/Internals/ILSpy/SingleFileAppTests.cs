#if NET5_0_OR_GREATER && PLATFORM_NEUTRAL
using Sentry.Internal.ILSpy;

namespace Sentry.Tests.Internals.ILSpy;

/// <summary>
/// Note the tests in this class rely on the SingleFileTestApp having being built. This will be done automatically if
/// SingleFileTestApp is included in the solution that dotnet test is running against. Otherwise, tests are skipped.
/// </summary>
public class SingleFileAppTests
{
    private static readonly string ValidBundleFile;
    private static readonly string InValidBundleFile;
    static SingleFileAppTests()
    {
        var currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var debugConfigMarker = "/bin/Debug/".OsAgnostic();
        var buildConfig = currentDirectory.Contains(debugConfigMarker) ? "Debug" : "Release";

        // Get the test root
        var folderMarker = "/sentry-dotnet/test/".OsAgnostic();
        var cutoff = currentDirectory.IndexOf(folderMarker, StringComparison.Ordinal) + folderMarker.Length;
        var testRoot = currentDirectory[..cutoff];

        // Note that these files will only exist if the SingleFileTestApp has been built.
        var validBundle = $"SingleFileTestApp/bin/{buildConfig}/{TargetFramework}/{RuntimeIdentifier}/publish/{SingleFileAppName}".OsAgnostic();
        ValidBundleFile = Path.Combine(testRoot, validBundle);

        var invalidBundle = $"SingleFileTestApp/bin/{buildConfig}/{TargetFramework}/{RuntimeIdentifier}/{SingleFileAppName}".OsAgnostic();
        InValidBundleFile = Path.Combine(testRoot, invalidBundle);
    }

#if NET9_0
    private static string TargetFramework => "net9.0";
#elif NET8_0
    private static string TargetFramework => "net8.0";
#elif NET7_0
    private static string TargetFramework => "net7.0";
#elif NET6_0
    private static string TargetFramework => "net6.0";
#elif NET5_0
    private static string TargetFramework => "net5.0";
#else
    // Adding a new TFM to the project? Include it above
#error "Target Framework not yet supported for single file apps"
#endif

    private static string SingleFileAppName => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? "SingleFileTestApp.exe"
        : "SingleFileTestApp";

    private static string RuntimeIdentifier =>
        RuntimeInformation.OSArchitecture switch
        {
            Architecture.Arm64 => $"{OsPlatform}-arm64",
            Architecture.X64 => $"{OsPlatform}-x64",
            Architecture.X86 => $"{OsPlatform}-x86",
            _ => throw new Exception("Unknown Architecture")
        };

    private static string OsPlatform =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "win"
            : RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                ? "linux"
                : RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                    ? "osx"
                    : throw new Exception("Unknown OS");

    [SkippableFact]
    public void ValidBundleFile_KnownModule_Returns_DebugInfo()
    {
        Skip.If(!File.Exists(ValidBundleFile));

        // Act
        var singleFileApp = SingleFileApp.FromFile(ValidBundleFile);

        // Assert
        singleFileApp.Should().NotBeNull();

        var knownTypeModule = typeof(int).Module;
        var debugImage = singleFileApp!.GetDebugImage(knownTypeModule);
        debugImage.Should().NotBeNull();
        using (new AssertionScope())
        {
            debugImage!.CodeFile.Should().NotBeNull();
            debugImage.CodeId.Should().NotBeNull();
            debugImage.DebugId.Should().NotBeNull();
            debugImage.DebugFile.Should().NotBeNull();
            debugImage.Type.Should().NotBeNull();
            debugImage.ModuleVersionId.Should().NotBeNull();
        }
    }

    [SkippableFact]
    public void InvalidBundleFile_ReturnsNull()
    {
        Skip.If(!File.Exists(InValidBundleFile));

        // Act
        var singleFileApp = SingleFileApp.FromFile(InValidBundleFile);

        // Assert
        Assert.Null(singleFileApp);
    }
}

#endif
