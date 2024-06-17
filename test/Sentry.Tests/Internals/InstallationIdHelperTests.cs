namespace Sentry.Tests.Internals;

public class InstallationIdHelperTests
{
    private class Fixture : IDisposable
    {
        private readonly TempDirectory _cacheDirectory;

        public InMemoryDiagnosticLogger Logger { get; }

        public SentryOptions Options { get; }

        public Fixture(Action<SentryOptions> configureOptions = null)
        {
            Logger = new InMemoryDiagnosticLogger();

            var fileSystem = new FakeFileSystem();
            _cacheDirectory = new TempDirectory(fileSystem);

            Options = new SentryOptions
            {
                Dsn = ValidDsn,
                CacheDirectoryPath = _cacheDirectory.Path,
                FileSystem = fileSystem,
                Release = "test",
                Debug = true,
                DiagnosticLogger = Logger
            };

            configureOptions?.Invoke(Options);
        }

        public InstallationIdHelper GetSut() =>
            new(Options, _cacheDirectory.Path);

        public void Dispose() => _cacheDirectory.Dispose();
    }

    private readonly Fixture _fixture = new();

    [Fact]
    public void GetMachineNameInstallationId_Hashed()
    {
        // Arrange
        var sut = _fixture.GetSut();

        // Act
        var installationId = InstallationIdHelper.GetMachineNameInstallationId();

        // Assert
        installationId.Should().NotBeNullOrWhiteSpace();
        installationId.Should().NotBeEquivalentTo(Environment.MachineName);
    }

    [Fact]
    public void GetMachineNameInstallationId_Idempotent()
    {
        // Arrange
        var sut = _fixture.GetSut();

        // Act
        var installationIds = Enumerable
            .Range(0, 10)
            .Select(_ => InstallationIdHelper.GetMachineNameInstallationId())
            .ToArray();

        // Assert
        installationIds.Distinct().Should().ContainSingle();
    }
}
