#if NET6_0_OR_GREATER
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

    private IAndroidAssemblyReader GetSut(bool isAssemblyStore, bool compressed, List<string> supportedAbis)
    {
        var apkPath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            "../../../Internals/",
            $"android-Store={isAssemblyStore}-Compressed={compressed}.apk");

        _output.WriteLine($"Checking if APK exists: {apkPath}");
        File.Exists(apkPath).Should().BeTrue();

        return AndroidAssemblyReaderFactory.Open(apkPath, supportedAbis, _logger);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CreatesCorrectReader(bool isAssemblyStore)
    {
        using var sut = GetSut(isAssemblyStore, compressed: true, supportedAbis: new());
        if (isAssemblyStore)
        {
            Assert.IsType<AndroidAssemblyStoreReader>(sut);
        }
        else
        {
            Assert.IsType<AndroidAssemblyDirectoryReader>(sut);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReturnsNullIfAssemblyDoesntExist(bool isAssemblyStore)
    {
        using var sut = GetSut(isAssemblyStore, compressed: true, supportedAbis: new() { "x86_64" });
        Assert.Null(sut.TryReadAssembly("NonExistent.dll"));
    }

    [Theory]
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
        using var sut = GetSut(isAssemblyStore, isCompressed, supportedAbis: new() { "x86_64" });

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
