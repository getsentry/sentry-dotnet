namespace Sentry.Tests;

public class GlobalSessionManagerTests
{
    private class Fixture
    {
        public InMemoryDiagnosticLogger Logger { get; }

        public SentryOptions Options { get; }

        public ISystemClock Clock { get; } = Substitute.For<ISystemClock>();

        public Func<string, PersistedSessionUpdate> PersistedSessionProvider { get; set; }

        public Fixture(Action<SentryOptions> configureOptions = null)
        {
            Clock.GetUtcNow().Returns(DateTimeOffset.Now);
            Logger = new InMemoryDiagnosticLogger();

            Options = new SentryOptions
            {
                Dsn = ValidDsn,
                Release = "test",
                Debug = true,
                DiagnosticLogger = Logger,
                CacheDirectoryPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()),
                FileSystem = new FakeFileSystem() // Keep all fileIO operations in memory
            };

            configureOptions?.Invoke(Options);
        }

        public GlobalSessionManager GetSut() =>
            new(
                Options,
                Clock,
                PersistedSessionProvider);
    }

    private readonly Fixture _fixture = new();

    [Fact]
    public void StartSession_ReleaseSet_CreatesNewSession()
    {
        // Arrange
        var sut = _fixture.GetSut();

        // Act
        var sessionUpdate = sut.StartSession();

        // Assert
        sessionUpdate.Should().NotBeNull();
        sessionUpdate?.Id.Should().NotBe(SentryId.Empty);
        sessionUpdate?.Release.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void StartSession_CacheDirectoryProvided_InstallationIdFileCreated()
    {
        // Arrange
        var sut = _fixture.GetSut();

        var filePath = Path.Combine(
            _fixture.Options.CacheDirectoryPath!,
            "Sentry",
            _fixture.Options.Dsn!.GetHashString(),
            ".installation");

        // Act
        sut.StartSession();

        // Assert
        Assert.True(_fixture.Options.FileSystem.FileExists(filePath));
    }

    [Fact]
    public void StartSession_CacheDirectoryProvidedButFileWriteDisabled_InstallationIdFileNotCreated()
    {
        // Arrange
        _fixture.Options.DisableFileWrite = true;
        var sut = _fixture.GetSut();

        var filePath = Path.Combine(
            _fixture.Options.CacheDirectoryPath!,
            "Sentry",
            _fixture.Options.Dsn!.GetHashString(),
            ".installation");

        // Act
        sut.StartSession();

        // Assert
        Assert.False(_fixture.Options.FileSystem.FileExists(filePath));
    }

    [SkippableFact]
    public void StartSession_CacheDirectoryNotProvided_InstallationIdFileCreated()
    {
        // Arrange
        _fixture.Options.CacheDirectoryPath = null;
        var filePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Sentry",
            _fixture.Options.Dsn!.GetHashString(),
            ".installation");

        // Act
        _fixture.GetSut().StartSession();

        // Assert
        Assert.True(_fixture.Options.FileSystem.FileExists(filePath));
    }

    [Fact]
    public void StartSession_CacheDirectoryNotProvidedAndFileWriteDisabled_InstallationIdFileNotCreated()
    {
        // Arrange
        _fixture.Options.DisableFileWrite = true;
        _fixture.Options.CacheDirectoryPath = null;

        var filePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Sentry",
            _fixture.Options.Dsn!.GetHashString(),
            ".installation");

        // Act
        _fixture.GetSut().StartSession();

        // Assert
        Assert.False(_fixture.Options.FileSystem.FileExists(filePath));
    }

    [Fact]
    public void StartSession_InstallationId_AlwaysSameId()
    {
        // Arrange
        var sut = _fixture.GetSut();

        // Act
        var sessionUpdates = Enumerable
            .Range(0, 15)
            .Select(_ => sut.StartSession())
            .ToArray();

        // Assert
        sessionUpdates.Select(s => s.DistinctId).Distinct().Should().ContainSingle();
    }

    [Fact]
    public void ReportError_ActiveSessionExists_ReturnsNewUpdateWithIncrementedErrorCount()
    {
        // Arrange
        var sut = _fixture.GetSut();

        sut.StartSession();

        // Act
        var sessionUpdate = sut.ReportError();

        // Assert
        sessionUpdate.Should().NotBeNull();
        sessionUpdate?.ErrorCount.Should().Be(1);
    }

    [Fact]
    public void ReportError_ActiveSessionExistsWithNonZeroErrorCount_DoesNotReturnNewUpdate()
    {
        // Arrange
        var sut = _fixture.GetSut();

        sut.StartSession();

        // Act
        sut.ReportError();
        var sessionUpdate = sut.ReportError();

        // Assert
        sessionUpdate.Should().BeNull();
    }

    [Fact]
    public void ReportError_ActiveSessionDoesNotExist_LogsOutError()
    {
        // Arrange
        var sut = _fixture.GetSut();

        // Act
        sut.ReportError();

        // Assert
        _fixture.Logger.Entries.Should().Contain(e =>
            e.Message == "There is no session active. Skipping updating the session as errored. Consider setting 'AutoSessionTracking = true' to enable Release Health and crash free rate." &&
            e.Level == SentryLevel.Debug);
    }

    [Fact]
    public void EndSession_ActiveSessionExists_ExitedStatus_EndsSession()
    {
        // Arrange
        var sut = _fixture.GetSut();

        sut.StartSession();
        var session = sut.CurrentSession;

        // Act
        var sessionUpdate = sut.EndSession(SessionEndStatus.Exited);

        // Assert
        session.Should().NotBeNull();
        sessionUpdate?.EndStatus.Should().Be(SessionEndStatus.Exited);
        sessionUpdate?.ErrorCount.Should().Be(0);
    }

    [Fact]
    public void EndSession_ActiveSessionExists_CrashedStatus_EndsSession()
    {
        // Arrange
        var sut = _fixture.GetSut();

        sut.StartSession();
        var session = sut.CurrentSession;

        // Act
        var sessionUpdate = sut.EndSession(SessionEndStatus.Crashed);

        // Assert
        session.Should().NotBeNull();
        sessionUpdate?.EndStatus.Should().Be(SessionEndStatus.Crashed);
        sessionUpdate?.ErrorCount.Should().Be(1);
    }

    [Fact]
    public void EndSession_ActiveSessionDoesNotExist_DoesNothing()
    {
        // Arrange
        var sut = _fixture.GetSut();

        // Act
        var endedSession = sut.EndSession(SessionEndStatus.Exited);

        // Assert
        endedSession.Should().BeNull();

        _fixture.Logger.Entries.Should().Contain(e =>
            e.Message == "Failed to end session because there is none active." &&
            e.Level == SentryLevel.Warning);
    }

    [Fact]
    public void TryRecoverPersistedSession_SessionNotStarted_ReturnsNull()
    {
        // Arrange
        var sut = _fixture.GetSut();

        // Act
        var persistedSessionUpdate = sut.TryRecoverPersistedSession();

        // Assert
        persistedSessionUpdate.Should().BeNull();
    }

    [Fact]
    public void TryRecoverPersistedSession_FileNotFoundException_LogDebug()
    {
        // Arrange
        var sut = _fixture.GetSut();
        sut = new GlobalSessionManager(
            _fixture.Options,
            persistedSessionProvider: _ => throw new FileNotFoundException());

        // Act
        sut.TryRecoverPersistedSession();

        // Assert
        _fixture.Logger.Entries.Should().Contain(e => e.Level == SentryLevel.Debug);
    }

    [Fact]
    public void TryRecoverPersistedSession_DirectoryNotFoundException_LogDebug()
    {
        // Arrange
        var sut = _fixture.GetSut();
        sut = new GlobalSessionManager(
            _fixture.Options,
            persistedSessionProvider: _ => throw new DirectoryNotFoundException());

        // Act
        sut.TryRecoverPersistedSession();

        // Assert
        _fixture.Logger.Entries.Should().Contain(e => e.Level == SentryLevel.Debug);
    }

    [Fact]
    public void TryRecoverPersistedSession_EndOfStreamException_LogError()
    {
        // Arrange
        var sut = _fixture.GetSut();
        sut = new GlobalSessionManager(
            _fixture.Options,
            persistedSessionProvider: _ => throw new EndOfStreamException());

        // Act
        sut.TryRecoverPersistedSession();

        // Assert
        _fixture.Logger.Entries.Should().Contain(e => e.Level == SentryLevel.Error);
    }

    [Fact]
    public void TryRecoverPersistedSession_SessionStarted_ReturnsLastSession()
    {
        // Arrange
        var sut = _fixture.GetSut();

        var timeOffset = DateTimeOffset.Now;
        _fixture.Clock.GetUtcNow().Returns(_ =>
        {
            timeOffset = timeOffset.AddSeconds(1);
            return timeOffset;
        });

        var sessionUpdate = sut.StartSession();

        // Act
        var persistedSessionUpdate = sut.TryRecoverPersistedSession()!;

        // Assert
        sessionUpdate.Should().NotBeNull();
        persistedSessionUpdate.EndStatus.Should().Be(SessionEndStatus.Abnormal);
        persistedSessionUpdate.Should().NotBeNull();
        persistedSessionUpdate.Should().BeEquivalentTo(sessionUpdate, o =>
        {
            o.Excluding(u => u.IsInitial);
            o.Excluding(u => u.Timestamp);
            o.Excluding(u => u.Duration);
            o.Excluding(u => u.SequenceNumber);
            o.Excluding(u => u.EndStatus);

            return o;
        });
        persistedSessionUpdate.IsInitial.Should().BeFalse();
        persistedSessionUpdate.Timestamp.Should().BeAfter(sessionUpdate!.Timestamp);
        persistedSessionUpdate.Duration.Should().BeGreaterThan(sessionUpdate!.Duration);
        persistedSessionUpdate.SequenceNumber.Should().Be(sessionUpdate!.SequenceNumber + 1);
    }

    [Fact]
    public void TryRecoverPersistedSession_SessionStarted_DidCrashDelegateNotProvided_EndsAsAbnormal()
    {
        // Arrange
        var sut = _fixture.GetSut();

        sut.StartSession();

        // Act
        var persistedSessionUpdate = sut.TryRecoverPersistedSession();

        // Assert
        persistedSessionUpdate.Should().NotBeNull();
        persistedSessionUpdate!.EndStatus.Should().Be(SessionEndStatus.Abnormal);
    }

    [Fact]
    public void TryRecoverPersistedSession_SessionStarted_CrashDelegateReturnsFalse_EndsAsAbnormal()
    {
        // Arrange
        _fixture.Options.CrashedLastRun = () => false;
        var sut = _fixture.GetSut();

        sut.StartSession();

        // Act
        var persistedSessionUpdate = sut.TryRecoverPersistedSession();

        // Assert
        persistedSessionUpdate.Should().NotBeNull();
        persistedSessionUpdate!.EndStatus.Should().Be(SessionEndStatus.Abnormal);
    }

    [Fact]
    public void TryRecoverPersistedSession_CrashDelegateReturnsTrueWithPauseTimestamp_EndsAsCrashed()
    {
        // Arrange
        _fixture.Options.CrashedLastRun = () => true;
        // Session was paused before persisted:
        var pausedTimestamp = DateTimeOffset.Now;
        _fixture.PersistedSessionProvider = _ => new PersistedSessionUpdate(
            AnySessionUpdate(),
            pausedTimestamp);

        var sut = _fixture.GetSut();

        // Act
        var persistedSessionUpdate = sut.TryRecoverPersistedSession();

        // Assert
        persistedSessionUpdate.Should().NotBeNull();
        persistedSessionUpdate!.EndStatus.Should().Be(SessionEndStatus.Crashed);
    }

    [Fact]
    public void TryRecoverPersistedSession_CrashDelegateIsNullWithPauseTimestamp_EndsAsExited()
    {
        // Arrange
        _fixture.Options.CrashedLastRun = null;
        // Session was paused before persisted:
        var pausedTimestamp = DateTimeOffset.Now;
        _fixture.PersistedSessionProvider = _ => new PersistedSessionUpdate(
            AnySessionUpdate(),
            pausedTimestamp);

        var sut = _fixture.GetSut();

        // Act
        var persistedSessionUpdate = sut.TryRecoverPersistedSession();

        // Assert
        persistedSessionUpdate.Should().NotBeNull();
        persistedSessionUpdate!.EndStatus.Should().Be(SessionEndStatus.Exited);
    }

    [Fact]
    public void TryRecoverPersistedSession_CrashDelegateIsNullWithoutPauseTimestamp_EndsAsAbnormal()
    {
        // Arrange
        _fixture.Options.CrashedLastRun = null;
        var pausedTimestamp = DateTimeOffset.Now;
        _fixture.PersistedSessionProvider = _ => new PersistedSessionUpdate(
            AnySessionUpdate(),
            // No pause timestamp:
            null);

        var sut = _fixture.GetSut();

        sut.StartSession();

        // Act
        var persistedSessionUpdate = sut.TryRecoverPersistedSession();

        // Assert
        persistedSessionUpdate.Should().NotBeNull();
        persistedSessionUpdate!.EndStatus.Should().Be(SessionEndStatus.Abnormal);
    }

    [Fact]
    public void TryRecoverPersistedSession_SessionStarted_CrashDelegateReturnsTrue_EndsAsCrashed()
    {
        // Arrange
        _fixture.Options.CrashedLastRun = () => true;
        var sut = _fixture.GetSut();

        sut.StartSession();

        // Act
        var persistedSessionUpdate = sut.TryRecoverPersistedSession();

        // Assert
        persistedSessionUpdate.Should().NotBeNull();
        persistedSessionUpdate!.EndStatus.Should().Be(SessionEndStatus.Crashed);
    }

    [Fact]
    public void TryRecoverPersistedSession_SessionPaused_EndsAsExited()
    {
        // Arrange
        var sut = _fixture.GetSut();

        sut.StartSession();
        sut.PauseSession();

        // Act
        var persistedSessionUpdate = sut.TryRecoverPersistedSession();

        // Assert
        persistedSessionUpdate.Should().NotBeNull();
        persistedSessionUpdate!.EndStatus.Should().Be(SessionEndStatus.Exited);
    }

    [Fact]
    public void TryRecoverPersistedSession_SessionEnded_ReturnsNull()
    {
        // Arrange
        var sut = _fixture.GetSut();

        sut.StartSession();
        sut.EndSession(SessionEndStatus.Exited);

        // Act
        var persistedSessionUpdate = sut.TryRecoverPersistedSession();

        // Assert
        persistedSessionUpdate.Should().BeNull();
    }

    public SessionUpdate TryRecoverPersistedSessionWithExceptionOnLastRun()
    {
        // Arrange
        var expectedCrashMessage = "Invoking CrashedLastRun failed.";
        var expectedException = new Exception();

        var sut = _fixture.GetSut();
        _fixture.Options.CrashedLastRun = () => throw expectedException;
        sut.StartSession();

        // Act
        var persistedSessionUpdate = sut.TryRecoverPersistedSession();

        // Assert
        persistedSessionUpdate.Should().NotBeNull();
        persistedSessionUpdate.EndStatus.Should().BeNull();
        _fixture.Logger.Entries.Should().Contain(entry =>
            entry.Level == SentryLevel.Error &&
            entry.Message == expectedCrashMessage &&
            entry.Exception == expectedException);
        return persistedSessionUpdate;
    }

    [Fact]
    public void TryRecoverPersistedSession_HasRecoveredUpdateAndCrashedLastRunFailed_RecoveredSessionCaptured()
    {
        TryRecoverPersistedSessionWithExceptionOnLastRun();
    }

    [Fact]
    public void MarkSessionAsUnhandled_ActiveSessionExists_MarksSessionAndPersists()
    {
        // Arrange
        var sut = _fixture.GetSut();
        sut.StartSession();
        var session = sut.CurrentSession;

        // Act
        sut.MarkSessionAsUnhandled();

        // Assert
        session.Should().NotBeNull();
        session!.IsMarkedAsPendingUnhandled.Should().BeTrue();

        // Session should still be active (not ended)
        sut.CurrentSession.Should().BeSameAs(session);
    }

    [Fact]
    public void MarkSessionAsUnhandled_NoActiveSession_LogsDebug()
    {
        // Arrange
        var sut = _fixture.GetSut();

        // Act
        sut.MarkSessionAsUnhandled();

        // Assert
        _fixture.Logger.Entries.Should().Contain(e =>
            e.Message == "There is no session active. Skipping marking session as unhandled." &&
            e.Level == SentryLevel.Debug);
    }

    [Fact]
    public void TryRecoverPersistedSession_WithPendingUnhandledAndNoCrash_EndsAsUnhandled()
    {
        // Arrange
        _fixture.Options.CrashedLastRun = () => false;
        _fixture.PersistedSessionProvider = _ => new PersistedSessionUpdate(
            AnySessionUpdate(),
            pauseTimestamp: null,
            pendingUnhandled: true);

        var sut = _fixture.GetSut();

        // Act
        var persistedSessionUpdate = sut.TryRecoverPersistedSession();

        // Assert
        persistedSessionUpdate.Should().NotBeNull();
        persistedSessionUpdate!.EndStatus.Should().Be(SessionEndStatus.Unhandled);
    }

    [Fact]
    public void TryRecoverPersistedSession_WithPendingUnhandledAndCrash_EscalatesToCrashed()
    {
        // Arrange
        _fixture.Options.CrashedLastRun = () => true;
        _fixture.PersistedSessionProvider = _ => new PersistedSessionUpdate(
            AnySessionUpdate(),
            pauseTimestamp: null,
            pendingUnhandled: true);

        var sut = _fixture.GetSut();

        // Act
        var persistedSessionUpdate = sut.TryRecoverPersistedSession();

        // Assert
        persistedSessionUpdate.Should().NotBeNull();
        persistedSessionUpdate!.EndStatus.Should().Be(SessionEndStatus.Crashed);
    }

    [Fact]
    public void TryRecoverPersistedSession_WithPendingUnhandledAndPauseTimestamp_EscalatesToCrashedIfCrashed()
    {
        // Arrange - Session was paused AND had pending unhandled, then crashed
        _fixture.Options.CrashedLastRun = () => true;
        var pausedTimestamp = DateTimeOffset.Now;
        _fixture.PersistedSessionProvider = _ => new PersistedSessionUpdate(
            AnySessionUpdate(),
            pausedTimestamp,
            pendingUnhandled: true);

        var sut = _fixture.GetSut();

        // Act
        var persistedSessionUpdate = sut.TryRecoverPersistedSession();

        // Assert
        // Crash takes priority over all other end statuses
        persistedSessionUpdate.Should().NotBeNull();
        persistedSessionUpdate!.EndStatus.Should().Be(SessionEndStatus.Crashed);
    }

    [Fact]
    public void EndSession_WithPendingUnhandledException_PreservesUnhandledStatus()
    {
        // Arrange
        var sut = _fixture.GetSut();
        sut.StartSession();
        sut.MarkSessionAsUnhandled();

        // Act - Try to end normally with Exited status
        var sessionUpdate = sut.EndSession(SessionEndStatus.Exited);

        // Assert - Should be overridden to Unhandled
        sessionUpdate.Should().NotBeNull();
        sessionUpdate!.EndStatus.Should().Be(SessionEndStatus.Unhandled);
    }

    [Fact]
    public void EndSession_WithPendingUnhandledAndCrashedStatus_UsesCrashedStatus()
    {
        // Arrange
        var sut = _fixture.GetSut();
        sut.StartSession();
        sut.MarkSessionAsUnhandled();

        // Act - Explicitly end with Crashed status
        var sessionUpdate = sut.EndSession(SessionEndStatus.Crashed);

        // Assert - Crashed status takes priority
        sessionUpdate.Should().NotBeNull();
        sessionUpdate!.EndStatus.Should().Be(SessionEndStatus.Crashed);
        sessionUpdate.ErrorCount.Should().Be(1);
    }

    [Fact]
    public void SessionEscalation_CompleteFlow_UnhandledThenCrash()
    {
        // Arrange - Simulate complete flow
        var sut = _fixture.GetSut();
        sut.StartSession();
        var originalSessionId = sut.CurrentSession!.Id;

        // Act 1: Mark as unhandled (game encounters exception but continues)
        sut.MarkSessionAsUnhandled();

        // Assert: Session still active with pending flag
        sut.CurrentSession.Should().NotBeNull();
        sut.CurrentSession!.Id.Should().Be(originalSessionId);
        sut.CurrentSession.IsMarkedAsPendingUnhandled.Should().BeTrue();

        // Act 2: Recover on next launch with crash detected
        _fixture.Options.CrashedLastRun = () => true;
        var recovered = sut.TryRecoverPersistedSession();

        // Assert: Session escalated from Unhandled to Crashed
        recovered.Should().NotBeNull();
        recovered!.EndStatus.Should().Be(SessionEndStatus.Crashed);
        recovered.Id.Should().Be(originalSessionId);
    }

    // A session update (of which the state doesn't matter for the test):
    private static SessionUpdate AnySessionUpdate()
        => new(
            SentryId.Create(),
            "did",
            DateTimeOffset.Now,
            "release",
            "env",
            "ip",
            "ua",
            0,
            true,
            DateTimeOffset.Now,
            1,
            null);
}
