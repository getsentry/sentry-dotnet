#if MEMORY_DUMP_SUPPORTED
namespace Sentry.Tests.Internals;

public class MemoryMonitorTests
{
    private HeapDumpTrigger NeverTrigger { get; } = (_, _) => false;
    private HeapDumpTrigger AlwaysTrigger { get; } = (_, _) => true;

    private class Fixture
    {
        private IGCImplementation GCImplementation { get; set; }

        public SentryOptions Options { get; set; } = new()
        {
            Dsn = ValidDsn,
            Debug = true,
            DiagnosticLogger = Substitute.For<IDiagnosticLogger>()
        };

        public Action<string> OnDumpCollected { get; set; } = _ => { };

        public Action OnCaptureDump { get; set; } = null;

        private const short ThresholdPercentage = 5;

        public static IGCImplementation MockGCImplementation()
        {
            var gc = Substitute.For<IGCImplementation>();
            gc.TotalAvailableMemoryBytes.Returns(1024 * 1024 * 1024);
            return gc;
        }

        public MemoryMonitor GetSut()
        {
            Options.DiagnosticLogger?.IsEnabled(Arg.Any<SentryLevel>()).Returns(true);
            return new MemoryMonitor(Options, OnDumpCollected, OnCaptureDump, GCImplementation ?? MockGCImplementation());
        }
    }

    private readonly Fixture _fixture = new();

    [Fact]
    public void Constructor_NoHeapdumpsConfigured_Throws()
    {
        var doNothing = new Action(() => { });

        // Arrange
        _fixture.Options.HeapDumpOptions = null;

        // Act
        MemoryMonitor sut = null;
        try
        {
            Assert.Throws<ArgumentException>(() => sut = new MemoryMonitor(
                _fixture.Options, _fixture.OnDumpCollected, doNothing, Fixture.MockGCImplementation()
            ));
        }
        finally
        {
            sut?.Dispose();
        }
    }

    [Fact]
    public void CheckMemoryUsage_Debounced_DoesNotCapture()
    {
        // Arrange
        _fixture.Options.EnableHeapDumps(
            AlwaysTrigger,
            Debouncer.PerApplicationLifetime(0) // always debounce
        );
        var dumpCaptured = false;
        _fixture.OnCaptureDump = () => dumpCaptured = true;
        using var sut = _fixture.GetSut();

        // Act
        sut.CheckMemoryUsage();

        // Assert
        dumpCaptured.Should().BeFalse();
    }

    [Fact]
    public void CheckMemoryUsage_NotTriggered_DoesNotCapture()
    {
        // Arrange
        _fixture.Options.EnableHeapDumps(
            NeverTrigger,
            Debouncer.PerApplicationLifetime(int.MaxValue) // never debounce
        );
        var dumpCaptured = false;
        _fixture.OnCaptureDump = () => dumpCaptured = true;
        using var sut = _fixture.GetSut();

        // Act
        sut.CheckMemoryUsage();

        // Assert
        dumpCaptured.Should().BeFalse();
    }

    [Fact]
    public void CheckMemoryUsage_TriggeredNotDebounced_Captures()
    {
        // Arrange
        _fixture.Options.EnableHeapDumps(
            AlwaysTrigger,
            Debouncer.PerApplicationLifetime(int.MaxValue) // never debounce
        );
        var dumpCaptured = false;
        _fixture.OnCaptureDump = () => dumpCaptured = true;
        using var sut = _fixture.GetSut();

        // Act
        sut.CheckMemoryUsage();

        // Assert
        dumpCaptured.Should().BeTrue();
    }

    [Fact]
    public void CaptureMemoryDump_DisableFileWrite_DoesNotCapture()
    {
        // Arrange
        _fixture.Options.EnableHeapDumps(AlwaysTrigger);
        _fixture.Options.DisableFileWrite = true;
        using var sut = _fixture.GetSut();
        var processRunner = Substitute.For<Action<string>>();

        // Act
        sut.CaptureMemoryDump(processRunner);

        // Assert
        _fixture.Options.ReceivedLogDebug("File write has been disabled via the options. Unable to create memory dump.");
        processRunner.DidNotReceive().Invoke(Arg.Any<string>());
    }

    [Fact]
    public void CaptureMemoryDump_UnresolvedDumpLocation_DoesNotCapture()
    {
        // Arrange
        _fixture.Options.EnableHeapDumps(AlwaysTrigger);
        _fixture.Options.FileSystem = Substitute.For<IFileSystem>();
        _fixture.Options.FileSystem.CreateDirectory(Arg.Any<string>()).Returns(false);
        using var sut = _fixture.GetSut();
        var processRunner = Substitute.For<Action<string>>();

        // Act
        sut.CaptureMemoryDump(processRunner);

        // Assert
        processRunner.DidNotReceive().Invoke(Arg.Any<string>());
    }

    [Fact]
    public void CaptureMemoryDump_CapturesDump()
    {
        // Arrange
        _fixture.Options.EnableHeapDumps(AlwaysTrigger);
        _fixture.Options.FileSystem = new FakeFileSystem();
        using var sut = _fixture.GetSut();
        var processRunner = Substitute.For<Action<string>>();

        // Act
        sut.CaptureMemoryDump(processRunner);

        // Assert
        processRunner.Received(1).Invoke(Arg.Any<string>());
    }

    [Fact]
    public void TryGetDumpLocation_DirectoryCreationFails_ReturnsNull()
    {
        // Arrange
        _fixture.Options.EnableHeapDumps(AlwaysTrigger);
        _fixture.Options.FileSystem = Substitute.For<IFileSystem>();
        _fixture.Options.FileSystem.CreateDirectory(Arg.Any<string>()).Returns(false);
        using var sut = _fixture.GetSut();

        // Act
        var result = sut.TryGetDumpLocation();

        // Assert
        result.Should().BeNull();
        _fixture.Options.FileSystem.Received().CreateDirectory(Arg.Any<string>());
        _fixture.Options.FileSystem.DidNotReceive().FileExists(Arg.Any<string>());
        _fixture.Options.ReceivedLogWarning("Failed to create a directory for memory dump ({0}).", Arg.Any<string>());
    }

    [Fact]
    public void TryGetDumpLocation_DumpFileExists_ReturnsNull()
    {
        // Arrange
        _fixture.Options.EnableHeapDumps(AlwaysTrigger);
        _fixture.Options.FileSystem = Substitute.For<IFileSystem>();
        _fixture.Options.FileSystem.CreateDirectory(Arg.Any<string>()).Returns(true);
        _fixture.Options.FileSystem.FileExists(Arg.Any<string>()).Returns(true);
        using var sut = _fixture.GetSut();

        // Act
        var result = sut.TryGetDumpLocation();

        // Assert
        result.Should().BeNull();
        _fixture.Options.FileSystem.Received().CreateDirectory(Arg.Any<string>());
        _fixture.Options.FileSystem.Received().FileExists(Arg.Any<string>());
        _fixture.Options.ReceivedLogWarning("Duplicate dump file detected.");
    }

    [Fact]
    public void TryGetDumpLocation_Exception_LogsError()
    {
        // Arrange
        _fixture.Options.EnableHeapDumps(AlwaysTrigger);
        _fixture.Options.FileSystem = Substitute.For<IFileSystem>();
        _fixture.Options.FileSystem.CreateDirectory(Arg.Any<string>()).Throws(_ => new Exception());
        using var sut = _fixture.GetSut();

        // Act
        var result = sut.TryGetDumpLocation();

        // Assert
        result.Should().BeNull();
        _fixture.Options.ReceivedLogError(Arg.Any<Exception>(), "Failed to resolve appropriate memory dump location.");
    }

    [Fact]
    public void TryGetDumpLocation_ReturnsFilePath()
    {
        // Arrange
        _fixture.Options.EnableHeapDumps(AlwaysTrigger);
        _fixture.Options.FileSystem = new FakeFileSystem();
        using var sut = _fixture.GetSut();

        // Act
        var result = sut.TryGetDumpLocation();

        // Assert
        result.Should().NotBeNull();
    }
}
#endif
