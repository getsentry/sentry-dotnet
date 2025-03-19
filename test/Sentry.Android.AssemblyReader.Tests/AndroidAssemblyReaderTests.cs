namespace Sentry.Android.AssemblyReader.Tests;

public class AndroidAssemblyReaderTests
{
    private readonly ITestOutputHelper _output;

#if NET9_0
    private static string TargetFramework => "net9.0";
#elif NET8_0
    private static string TargetFramework => "net8.0";
#else
    // Adding a new TFM to the project? Include it above
#error "Target Framework not yet supported for AndroidAssemblyReader"
#endif

    public AndroidAssemblyReaderTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private IAndroidAssemblyReader GetSut(bool isAssemblyStore, bool isCompressed)
    {
#if ANDROID
        var logger = new TestOutputDiagnosticLogger(_output);
        return AndroidHelpers.GetAndroidAssemblyReader(logger)!;
#else
        var apkPath =
            Path.GetFullPath(Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
                "..", "..", "..", "TestAPKs",
                $"{TargetFramework}-android-Store={isAssemblyStore}-Compressed={isCompressed}.apk"));

        _output.WriteLine($"Checking if APK exists: {apkPath}");
        File.Exists(apkPath).Should().BeTrue();

        string[] supportedAbis = { "x86_64" };
        return AndroidAssemblyReaderFactory.Open(apkPath, supportedAbis,
            logger: (message, args) => _output.WriteLine(message, args));
#endif
    }

    [SkippableTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void CreatesCorrectReader(bool isAssemblyStore)
    {
#if ANDROID
        Skip.If(true, "It's unknown whether the current Android app APK is an assembly store or not.");
#endif
        using var sut = GetSut(isAssemblyStore, isCompressed: true);
        if (isAssemblyStore && TargetFramework == "net9.0")
        {
            Assert.IsType<V2.AndroidAssemblyStoreReaderV2>(sut);
        }
        else if (isAssemblyStore && TargetFramework == "net8.0")
        {
            Assert.IsType<V1.AndroidAssemblyStoreReaderV1>(sut);
        }
        else
        {
            Assert.IsType<AndroidAssemblyDirectoryReader>(sut);
        }
    }

    [SkippableTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReturnsNullIfAssemblyDoesntExist(bool isAssemblyStore)
    {
        using var sut = GetSut(isAssemblyStore, isCompressed: true);
        Assert.Null(sut.TryReadAssembly("NonExistent.dll"));
    }

    [SkippableTheory]
    [InlineData(false, true, "Mono.Android.dll")]
    [InlineData(false, false, "Mono.Android.dll")]
    [InlineData(false, true, "System.Runtime.dll")]
    [InlineData(false, false, "System.Runtime.dll")]
    [InlineData(true, true, "Mono.Android.dll")]
    [InlineData(true, false, "Mono.Android.dll")]
    [InlineData(true, true, "System.Runtime.dll")]
    [InlineData(true, false, "System.Runtime.dll")]
    public void ReadsAssembly(bool isAssemblyStore, bool isCompressed, string assemblyName)
    {
#if ANDROID
        // No need to run all combinations - we only test the current APK which is (likely) compressed assembly store.
        Skip.If(!isAssemblyStore);
        Skip.If(!isCompressed);
#endif
#if NET9_0_OR_GREATER
        // Building without an assembly store is not yet supported in net9.0 and above
        Skip.If(!isAssemblyStore);
#endif
        using var sut = GetSut(isAssemblyStore, isCompressed);

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
