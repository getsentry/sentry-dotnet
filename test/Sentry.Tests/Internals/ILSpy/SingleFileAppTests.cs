using Sentry.Internal.ILSpy;

namespace Sentry.Tests.Internals.ILSpy;

#if NETCOREAPP3_0_OR_GREATER && PLATFORM_NEUTRAL

/// <summary>
/// Note the tests in this class rely on the SingleFileTestApp having being built. This will be done automatically if
/// SingleFileTestApp is included in the solution that dotnet test is running against. Otherwise, tests are skipped.
/// </summary>
public class SingleFileAppTests
{
    private static readonly string PathSeparator;
    private static readonly string ValidBundleFile;
    private static readonly string InValidBundleFile;
    static SingleFileAppTests()
    {
        PathSeparator = Path.DirectorySeparatorChar.ToString();

        var currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var debugConfigMarker = "/bin/Debug/".OsAgnostic();
        var buildConfig = currentDirectory.Contains(debugConfigMarker) ? "Debug" : "Release";

        // Get the test root
        var folderMarker = "/sentry-dotnet/test/".OsAgnostic();
        var cutoff = currentDirectory.IndexOf(folderMarker, StringComparison.Ordinal) + folderMarker.Length;
        var testRoot = currentDirectory[..cutoff];

        // Note that these files will only exist if the SingleFileTestApp has been built.
        var validBundle = $"SingleFileTestApp/bin/{buildConfig}/net7.0/win-x64/publish/SingleFileTestApp.exe".OsAgnostic();
        ValidBundleFile = Path.Combine(testRoot, validBundle);

        var invalidBundle = $"SingleFileTestApp/bin/{buildConfig}/net7.0/win-x64/SingleFileTestApp.exe".OsAgnostic();
        InValidBundleFile = Path.Combine(testRoot, invalidBundle);
    }

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
            debugImage.DebugChecksum.Should().NotBeNull();
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

internal static class PathExtensions
{
    private static readonly string PathSeparator = Path.DirectorySeparatorChar.ToString();

    public static string OsAgnostic(this string path) => path.Replace("/", PathSeparator);
    public static string TrimLeadingPathSeparator(this string path) => path[..1] == PathSeparator ? path[1..] : path;
}
