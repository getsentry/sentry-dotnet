using Sentry.Android.AssemblyReader.V2;

namespace Sentry.Android.AssemblyReader.Tests;

public class AndroidAssemblyReaderTests
{
    private readonly ITestOutputHelper _output;
#if NET10_0
    private static string TargetFramework => "net10.0";
#elif NET9_0
    private static string TargetFramework => "net9.0";
#else
    // Adding a new TFM to the project? Include it above
#error "Target Framework not yet supported for AndroidAssemblyReader"
#endif

    public AndroidAssemblyReaderTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private IAndroidAssemblyReader GetSut(bool isAot, bool isAssemblyStore, bool isCompressed)
    {
#if ANDROID
        var logger = new TestOutputDiagnosticLogger(_output);
        return AndroidHelpers.GetAndroidAssemblyReader(logger)!;
#else
        var apkPath =
            Path.GetFullPath(Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
                "..", "..", "..", "TestAPKs",
                $"{TargetFramework}-android-A={isAot}-S={isAssemblyStore}-C={isCompressed}.apk"));

        _output.WriteLine($"Checking if APK exists: {apkPath}");
        File.Exists(apkPath).Should().BeTrue();

        // Note: This needs to match the RID used when publishing the test APK
        string[] supportedAbis = { "x86_64" };
        return AndroidAssemblyReaderFactory.Open(apkPath, supportedAbis,
            logger: (_, message, args) => _output.WriteLine(message, args));
#endif
    }

    [SkippableFact]
    public void CreatesCorrectStoreReader()
    {
#if ANDROID
        Skip.If(true, "It's unknown whether the current Android app APK is an assembly store or not.");
#endif
        using var sut = GetSut(isAot: false, isAssemblyStore: true, isCompressed: true);
        switch (TargetFramework)
        {
            case "net10.0":
                Assert.IsType<AndroidAssemblyStoreReaderV2>(sut);
                break;
            case "net9.0":
                Assert.IsType<AndroidAssemblyStoreReaderV2>(sut);
                break;
            default:
                throw new NotSupportedException($"Unsupported target framework: {TargetFramework}");
        }
    }

    [SkippableFact]
    public void CreatesCorrectArchiveReader()
    {
#if ANDROID
        Skip.If(true, "It's unknown whether the current Android app APK is an assembly store or not.");
#endif
        using var sut = GetSut(isAot: false, isAssemblyStore: false, isCompressed: true);
        switch (TargetFramework)
        {
            case "net10.0":
                Assert.IsType<AndroidAssemblyDirectoryReaderV2>(sut);
                break;
            case "net9.0":
                Assert.IsType<AndroidAssemblyDirectoryReaderV2>(sut);
                break;
            default:
                throw new NotSupportedException($"Unsupported target framework: {TargetFramework}");
        }
    }

    [SkippableTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReturnsNullIfAssemblyDoesntExist(bool isAssemblyStore)
    {
        using var sut = GetSut(isAot: false, isAssemblyStore, isCompressed: true);
        Assert.Null(sut.TryReadAssembly("NonExistent.dll"));
    }

    public static IEnumerable<object[]> ReadsAssemblyPermutations =>
        from isAot in new[] { true, false }
        from isStore in new[] { true, false }
        from isCompressed in new[] { true, false }
        from assemblyName in new[] { "Mono.Android.dll", "System.Private.CoreLib.dll" }
        select new object[] { isAot, isStore, isCompressed, assemblyName };

    [SkippableTheory]
    [MemberData(nameof(ReadsAssemblyPermutations))]
    public void ReadsAssembly(bool isAot, bool isAssemblyStore, bool isCompressed, string assemblyName)
    {
#if ANDROID
        // No need to run all combinations - we only test the current APK which is likely JIT compressed assembly store.
        Skip.If(isAot);
        Skip.If(!isAssemblyStore);
        Skip.If(!isCompressed);
#endif
        using var sut = GetSut(isAot, isAssemblyStore, isCompressed);

        var peReader = sut.TryReadAssembly(assemblyName);
        Assert.NotNull(peReader);
        Assert.True(peReader.HasMetadata);

        var headers = peReader.PEHeaders;
        Assert.True(headers.IsDll);
        headers.MetadataSize.Should().BeGreaterThan(0);
        Assert.NotNull(headers.PEHeader);
        headers.PEHeader.SizeOfImage.Should().BeGreaterThan(0);
        var debugDirs = peReader.ReadDebugDirectory();
        debugDirs.Length.Should().BeGreaterThan(0);
    }
}
