#if NET6_0_OR_GREATER && !__IOS__
using System.Reflection;
using Sentry.Testing;

namespace Sentry.Tests.Internals;

public class AndroidAssemblyReaderTests
{
    private readonly ITestOutputHelper _output;
    private readonly TestOutputDiagnosticLogger _logger;

    public AndroidAssemblyReaderTests(ITestOutputHelper output)
    {
        _output = output;
        _logger = new(output);
    }

    private IAndroidAssemblyReader GetSut(bool isAssemblyStore, bool isCompressed)
    {
#if ANDROID
        // On Android, this tests the current app APK.
        var apkPath = Environment.CommandLine;
        // AndroidBuild not available here - We can't use the usual code
        // var supportedAbis = AndroidBuild.SupportedAbis ?? new List<string> { AndroidBuild.CpuAbi ?? "" };
        var supportedAbis = new List<string> { "x86_64" };
#else
        var apkPath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            "../../../Internals/",
            $"android-Store={isAssemblyStore}-Compressed={isCompressed}.apk");
        var supportedAbis = new List<string> { "x86_64" };
#endif
        _output.WriteLine($"Checking if APK exists: {apkPath}");
        File.Exists(apkPath).Should().BeTrue();

        return AndroidAssemblyReaderFactory.Open(apkPath, supportedAbis, _logger);
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
        if (isAssemblyStore)
        {
            Assert.IsType<AndroidAssemblyStoreReader>(sut);
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
    [InlineData(false, true, "System.Threading.dll")]
    [InlineData(false, false, "System.Threading.dll")]
    [InlineData(true, true, "Mono.Android.dll")]
    [InlineData(true, false, "Mono.Android.dll")]
    [InlineData(true, true, "System.Threading.dll")]
    [InlineData(true, false, "System.Threading.dll")]
    public void ReadsAssembly(bool isAssemblyStore, bool isCompressed, string assemblyName)
    {
#if ANDROID
        // No need to run all combinations - we only test the current APK which is (likely) compressed assembly store.
        Skip.If(!isAssemblyStore);
        Skip.If(!isCompressed);
#endif
        using var sut = GetSut(isAssemblyStore, isCompressed);

        var peReader = sut.TryReadAssembly(assemblyName);
        Assert.NotNull(peReader);
        Assert.True(peReader.HasMetadata);

        var headers = peReader.PEHeaders;
        Assert.True(headers.IsDll);
        headers.MetadataSize.Should().BeGreaterThan(0);
        headers.PEHeader.SizeOfImage.Should().BeGreaterThan(0);
        var debugDirs = peReader.ReadDebugDirectory();
        debugDirs.Length.Should().BeGreaterThan(0);
    }
}
#endif
