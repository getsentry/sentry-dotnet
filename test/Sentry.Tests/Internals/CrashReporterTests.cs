#if PLATFORM_NEUTRAL
namespace Sentry.Tests.Internals;

public class CrashReporterTests
{
    private class Fixture
    {
        public FakeFileSystem FileSystem { get; } = new();
        public SentryOptions Options { get; }
        public List<ProcessStartInfo> Started { get; } = new();
        public Action<ProcessStartInfo> StartProcess { get; set; }

        public Fixture()
        {
            Options = new SentryOptions
            {
                Dsn = ValidDsn,
                FileSystem = FileSystem,
                CrashReporterPath = Path.Combine(Path.GetTempPath(), "crash-reporter")
            };
            StartProcess = Started.Add;
            FileSystem.CreateDirectory(Path.GetTempPath());
            FileSystem.WriteAllTextToFile(Options.CrashReporterPath, "");
        }

        public CrashReporter GetSut() => new(Options, StartProcess);
    }

    private readonly Fixture _fixture = new();

    [Fact]
    public void TryLaunch_NoPath_ReturnsFalse()
    {
        _fixture.Options.CrashReporterPath = null;
        var sut = _fixture.GetSut();

        var result = sut.TryLaunch(Envelope.FromEvent(new SentryEvent()));

        result.Should().BeFalse();
        _fixture.Started.Should().BeEmpty();
    }

    [Fact]
    public void TryLaunch_MissingReporter_ReturnsFalse()
    {
        _fixture.Options.CrashReporterPath = Path.Combine(Path.GetTempPath(), "missing");
        var sut = _fixture.GetSut();

        var result = sut.TryLaunch(Envelope.FromEvent(new SentryEvent()));

        result.Should().BeFalse();
        _fixture.Started.Should().BeEmpty();
    }

    [Fact]
    public void TryLaunch_Reporter_PassesEnvelopePath()
    {
        var @event = new SentryEvent();
        var sut = _fixture.GetSut();

        var result = sut.TryLaunch(Envelope.FromEvent(@event));

        result.Should().BeTrue();
        var file = Path.Combine(Path.GetTempPath(), "Sentry", "external", $"{@event.EventId}.envelope");
        _fixture.FileSystem.FileExists(file).Should().BeTrue();
        var startInfo = _fixture.Started.Should().ContainSingle().Subject;
        startInfo.FileName.Should().Be(_fixture.Options.CrashReporterPath);
        startInfo.UseShellExecute.Should().BeFalse();
        GetArguments(startInfo).Should().Be($"\"{file}\"");
    }

    [Fact]
    public void TryLaunch_CacheDirectoryPath_WritesEnvelopeToCacheDirectory()
    {
        _fixture.Options.CacheDirectoryPath = Path.Combine(Path.GetTempPath(), "cache");
        var @event = new SentryEvent();
        var sut = _fixture.GetSut();

        sut.TryLaunch(Envelope.FromEvent(@event));

        var file = Path.Combine(_fixture.Options.TryGetDsnSpecificCacheDirectoryPath()!, "external", $"{@event.EventId}.envelope");
        _fixture.FileSystem.FileExists(file).Should().BeTrue();
    }

    [Fact]
    public void TryLaunch_Reporter_AddsDsnHeader()
    {
        var @event = new SentryEvent();
        var sut = _fixture.GetSut();

        sut.TryLaunch(Envelope.FromEvent(@event));

        var file = Path.Combine(Path.GetTempPath(), "Sentry", "external", $"{@event.EventId}.envelope");
        var header = _fixture.FileSystem.ReadAllTextFromFile(file).Split('\n')[0];
        using var json = JsonDocument.Parse(header);
        json.RootElement.GetProperty("dsn").GetString().Should().Be(ValidDsn);
    }

    [Fact]
    public void TryLaunch_StartFails_ReturnsFalse()
    {
        _fixture.StartProcess = _ => throw new InvalidOperationException();
        var sut = _fixture.GetSut();

        var result = sut.TryLaunch(Envelope.FromEvent(new SentryEvent()));

        result.Should().BeFalse();
    }

    [Fact]
    public void TryLaunch_WriteFails_ReturnsFalse()
    {
        var fileSystem = Substitute.For<IFileSystem>();
        fileSystem.FileExists(_fixture.Options.CrashReporterPath!).Returns(true);
        _fixture.Options.FileSystem = fileSystem;
        var sut = _fixture.GetSut();

        var result = sut.TryLaunch(Envelope.FromEvent(new SentryEvent()));

        result.Should().BeFalse();
        _fixture.Started.Should().BeEmpty();
    }

    [PlatformFact(Platform.MacOS)]
    public void TryLaunch_Bundle_OpensBundle()
    {
        _fixture.Options.CrashReporterPath = Path.Combine(Path.GetTempPath(), "Crash Reporter.app");
        _fixture.FileSystem.CreateDirectory(_fixture.Options.CrashReporterPath);
        var @event = new SentryEvent();
        var sut = _fixture.GetSut();

        sut.TryLaunch(Envelope.FromEvent(@event));

        var file = Path.Combine(Path.GetTempPath(), "Sentry", "external", $"{@event.EventId}.envelope");
        var startInfo = _fixture.Started.Should().ContainSingle().Subject;
        startInfo.FileName.Should().Be("/usr/bin/open");
        GetArguments(startInfo).Should().Be($"\"-a\" \"{_fixture.Options.CrashReporterPath}\" \"--args\" \"{file}\"");
    }

    [SkippableFact]
    public void TryLaunch_AppNotOnMacOS_LaunchesApp()
    {
        Skip.If(RuntimeInformation.IsOSPlatform(OSPlatform.OSX));
        _fixture.Options.CrashReporterPath = Path.Combine(Path.GetTempPath(), "Crash Reporter.app");
        _fixture.FileSystem.WriteAllTextToFile(_fixture.Options.CrashReporterPath, "");
        var sut = _fixture.GetSut();

        sut.TryLaunch(Envelope.FromEvent(new SentryEvent()));

        var startInfo = _fixture.Started.Should().ContainSingle().Subject;
        startInfo.FileName.Should().Be(_fixture.Options.CrashReporterPath);
    }

    private static string GetArguments(ProcessStartInfo startInfo) =>
#if NETCOREAPP2_1_OR_GREATER
        string.Join(" ", startInfo.ArgumentList.Select(argument => $"\"{argument}\""));
#else
        startInfo.Arguments;
#endif
}
#endif
