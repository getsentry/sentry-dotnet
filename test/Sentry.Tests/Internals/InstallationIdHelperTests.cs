using System.IO.Abstractions.TestingHelpers;

namespace Sentry.Tests.Internals;

public class InstallationIdHelperTests
{
    private class Fixture : IDisposable
    {
        private readonly TempDirectory _cacheDirectory;

        public IDiagnosticLogger Logger { get; }

        public SentryOptions Options { get; }

        public Fixture(Action<SentryOptions> configureOptions = null)
        {
            Logger = Substitute.For<IDiagnosticLogger>();

            _cacheDirectory = new TempDirectory();

            Options = new SentryOptions
            {
                Dsn = ValidDsn,
                CacheDirectoryPath = _cacheDirectory.Path,
                Release = "test",
                Debug = true,
                DiagnosticLogger = Logger,
                // This keeps all writing-to-file operations in memory instead of actually writing to disk
                FileSystem = new FakeFileSystem()
            };

            configureOptions?.Invoke(Options);
        }

        public InstallationIdHelper GetSut() => new(Options);

        public void Dispose() => _cacheDirectory.Dispose();
    }

    private readonly Fixture _fixture = new();

    [Fact]
    public void GetMachineNameInstallationId_Hashed()
    {
        // Act
        var installationId = InstallationIdHelper.GetMachineNameInstallationId();

        // Assert
        installationId.Should().NotBeNullOrWhiteSpace();
        installationId.Should().NotBeEquivalentTo(Environment.MachineName);
    }

    [Fact]
    public void GetMachineNameInstallationId_Idempotent()
    {
        // Act
        var installationIds = Enumerable
            .Range(0, 10)
            .Select(_ => InstallationIdHelper.GetMachineNameInstallationId())
            .ToArray();

        // Assert
        installationIds.Distinct().Should().ContainSingle();
    }

    [Fact]
    public void TryGetInstallationId_CachesInstallationId()
    {
        // Arrange
        _fixture.Logger.IsEnabled(Arg.Any<SentryLevel>()).Returns(true);
        var installationIdHelper = _fixture.GetSut();

        // Act
        var installationId1 = installationIdHelper.TryGetInstallationId();

        // Assert
        installationId1.Should().NotBeNullOrWhiteSpace();
        _fixture.Logger.Received(1).Log(SentryLevel.Debug, "Resolved installation ID '{0}'.", null, Arg.Any<string>());

        // Arrange
        _fixture.Logger.ClearReceivedCalls();

        // Act
        var installationId2 = installationIdHelper.TryGetInstallationId();

        // Assert
        installationId2.Should().Be(installationId1);
        _fixture.Logger.Received(0).Log(SentryLevel.Debug, "Resolved installation ID '{0}'.", null, Arg.Any<string>());
    }
}
