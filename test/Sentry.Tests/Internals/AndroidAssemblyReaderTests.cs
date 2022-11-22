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

    private IAndroidAssemblyReader GetSut(bool isAssemblyStore, List<string> supportedAbis)
    {
        var apkPath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            "../../../Internals/",
            $"android-AssemblyStore={isAssemblyStore}.apk");

        _output.WriteLine($"APK for isAssemblyStore={isAssemblyStore}: {apkPath}");
        File.Exists(apkPath).Should().BeTrue();

        return AndroidAssemblyReaderFactory.Open(apkPath, supportedAbis, _logger);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CreatesCorrectReader(bool isAssemblyStore)
    {
        using var sut = GetSut(isAssemblyStore, new());
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
        using var sut = GetSut(isAssemblyStore, new() { "x86_64" });
        Assert.Null(sut.TryReadAssembly("NonExistent.dll"));
    }

    [Theory]
    [InlineData(false)]
    // [InlineData(true)]
    public void ReadsCommonAssembly(bool isAssemblyStore)
    {
        using var sut = GetSut(isAssemblyStore, new() { "x86_64" });
        var peReader = sut.TryReadAssembly("Mono.Android.dll");
        Assert.NotNull(peReader);
    }

    [Theory]
    [InlineData(false)]
    // [InlineData(true)]
    public void ReadsArchitectureSpecificAssembly(bool isAssemblyStore)
    {
        using var sut = GetSut(isAssemblyStore, new() { "x86_64" });
        var peReader = sut.TryReadAssembly("System.Threading.dll");
        Assert.NotNull(peReader);
    }
}
