using Sentry.Android.AssemblyReader.V2;

namespace Sentry.Android.AssemblyReader.Tests;

public class AndroidAssemblyReaderTests
{
    private readonly ITestOutputHelper _output;

#if NET11_0
    private static string TargetFramework => "net11.0";
#elif NET10_0
    private static string TargetFramework => "net10.0";
#else
    // Adding a new TFM to the project? Include it above
#error "Target Framework not yet supported for AndroidAssemblyReader"
#endif

    // .NET 11 Android moved to CoreCLR and emits v4 assembly stores, which our vendored
    // reader does not understand yet - it also changes ELF payload discovery, so even the
    // non-store APKs fail. Tracked by https://github.com/getsentry/sentry-dotnet/issues/5454;
    // re-enable these once that port lands.
    private const string StoreV4SkipReason =
        "Android assembly store v4 (.NET 11 / CoreCLR) is not supported yet - see getsentry/sentry-dotnet#5454";
#if NET11_0_OR_GREATER && !ANDROID
    private const bool StoreV4Unsupported = true;
#else
    private const bool StoreV4Unsupported = false;
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
        Skip.If(StoreV4Unsupported, StoreV4SkipReason);
#if ANDROID
        Skip.If(true, "It's unknown whether the current Android app APK is an assembly store or not.");
#endif
        using var sut = GetSut(isAot: false, isAssemblyStore: true, isCompressed: true);
        switch (TargetFramework)
        {
            case "net11.0":
                Assert.IsType<AndroidAssemblyStoreReader>(sut);
                break;
            case "net10.0":
                Assert.IsType<AndroidAssemblyStoreReader>(sut);
                break;
            default:
                throw new NotSupportedException($"Unsupported target framework: {TargetFramework}");
        }
    }

    [SkippableFact]
    public void CreatesCorrectArchiveReader()
    {
        Skip.If(StoreV4Unsupported, StoreV4SkipReason);
#if ANDROID
        Skip.If(true, "It's unknown whether the current Android app APK is an assembly store or not.");
#endif
        using var sut = GetSut(isAot: false, isAssemblyStore: false, isCompressed: true);
        switch (TargetFramework)
        {
            case "net11.0":
                Assert.IsType<AndroidAssemblyDirectoryReader>(sut);
                break;
            case "net10.0":
                Assert.IsType<AndroidAssemblyDirectoryReader>(sut);
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
        Skip.If(StoreV4Unsupported, StoreV4SkipReason);
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
        Skip.If(StoreV4Unsupported, StoreV4SkipReason);
#if ANDROID
        // No need to run all combinations - we only test the current APK which is likely JIT compressed assembly store.
        Skip.If(isAot);
        Skip.If(!isAssemblyStore);
        Skip.If(!isCompressed);
#elif NET11_0_OR_GREATER
        // .NET 11 removed the Mono runtime for Android (NETSDK1242) and RunAOTCompilation is
        // Mono-only, so no AOT APK can be produced to read. See the APK matrix in the csproj.
        Skip.If(isAot);
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
