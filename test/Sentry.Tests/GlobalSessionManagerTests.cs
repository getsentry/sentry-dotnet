using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using NSubstitute;
using Sentry.Infrastructure;
using Sentry.Internal;
using Sentry.Internal.Extensions;
using Sentry.Testing;
using Xunit;

namespace Sentry.Tests
{
    public class GlobalSessionManagerTests
    {
        private class Fixture : IDisposable
        {
            private readonly TempDirectory _cacheDirectory = new();

            public InMemoryDiagnosticLogger Logger { get; }

            public SentryOptions Options { get; }

            public ISentryClient Client { get; }

            public ISystemClock Clock { get; }

            public Func<string, PersistedSessionUpdate> PersistedSessionProvider { get; }

            public Fixture(Action<SentryOptions> configureOptions = null)
            {
                Logger = new InMemoryDiagnosticLogger();

                Options = new SentryOptions
                {
                    Dsn = DsnSamples.ValidDsnWithoutSecret,
                    CacheDirectoryPath = _cacheDirectory.Path,
                    Release = "test",
                    Debug = true,
                    DiagnosticLogger = Logger
                };

                Client = Substitute.For<ISentryClient>();

                configureOptions?.Invoke(Options);
            }

            public GlobalSessionManager GetSut() =>
                new(
                    Options,
                    Client,
                    Substitute.For<IInternalScopeManager>(),
                    Clock,
                    PersistedSessionProvider);

            public void Dispose() => _cacheDirectory.Dispose();
        }

        private readonly Fixture _fixture = new();

        [Fact]
        public void StartSession_ReleaseSet_CreatesNewSession()
        {
            // Arrange
            var sut = _fixture.GetSut();

            // Act
            sut.StartSession();
            var sessionUpdate = sut.CurrentSession;


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
            File.Exists(filePath).Should().BeTrue();
        }

        [Fact]
        public void StartSession_CacheDirectoryNotProvided_InstallationIdFileCreated()
        {
            // Arrange
            _fixture.Options.CacheDirectoryPath = null;
            var sut = _fixture.GetSut();

            var filePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Sentry",
                _fixture.Options.Dsn!.GetHashString(),
                ".installation");

            // Act
            sut.StartSession();

            // Assert
            File.Exists(filePath).Should().BeTrue();
        }

        [Fact]
        public void StartSession_InstallationId_AlwaysSameId()
        {
            // Arrange
            var sut = _fixture.GetSut();

            // Act
            var sessionUpdates = Enumerable
                .Range(0, 15)
                .Select(_ => { sut.StartSession();
                    return sut.CurrentSession;
                })
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
            sut.ReportError();
            var sessionUpdate = sut.CurrentSession;

            // Assert
            sessionUpdate.Should().NotBeNull();
            sessionUpdate?.ErrorCount.Should().Be(1);
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
                e.Message == "Failed to report an error on a session because there is none active." &&
                e.Level == SentryLevel.Debug);
        }

        [Fact]
        public void EndSession_ActiveSessionExists_EndsSession()
        {
            // Arrange
            var sessionsUpdate = new List<SessionUpdate>();
            var sut = _fixture.GetSut();
            _fixture.Client.When(client => client.CaptureSession(Arg.Any<SessionUpdate>()))
                .Do(callback => sessionsUpdate.Add(callback.Arg<SessionUpdate>()));

            sut.StartSession();
            var session = sut.CurrentSession;

            // Act
            sut.EndSession(SessionEndStatus.Exited);

            // Assert
            session.Should().NotBeNull();
            Assert.Equal(2, sessionsUpdate.Count);
            Assert.Equal(SessionEndStatus.Exited, sessionsUpdate.Last().EndStatus);
        }

        [Fact]
        public void EndSession_ActiveSessionDoesNotExist_DoesNothing()
        {
            // Arrange
            var sut = _fixture.GetSut();

            // Act
            sut.EndSession(SessionEndStatus.Exited);

            // Assert
            _fixture.Client.Received(0).CaptureSession(Arg.Any<SessionUpdate>());
            _fixture.Logger.Entries.Should().Contain(e =>
                e.Message == "Failed to end session because there is none active." &&
                e.Level == SentryLevel.Debug);
        }

        [Fact]
        public void GetMachineNameInstallationId_Hashed()
        {
            // Arrange
            var sut = _fixture.GetSut();

            // Act
            var installationId = sut.GetMachineNameInstallationId();

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
                .Select(_ => sut.GetMachineNameInstallationId())
                .ToArray();

            // Assert
            installationIds.Distinct().Should().ContainSingle();
        }

        [Fact]
        public void TryGetPersistentInstallationId_SessionNotStarted_ReturnsNull()
        {
            // Arrange
            var sut = _fixture.GetSut();

            // Act
            var persistedSessionUpdate = sut.TryRecoverPersistedSession();

            // Assert
            persistedSessionUpdate.Should().BeNull();
        }

        [Fact]
        public void TryGetPersistentInstallationId_FileNotFoundException_LogDebug()
        {
            // Arrange
            var sut = _fixture.GetSut();
            sut = new GlobalSessionManager(
                _fixture.Options,
                _fixture.Client,
                Substitute.For<IInternalScopeManager>(),
                persistedSessionProvider: _ => throw new FileNotFoundException());

            // Act
            sut.TryRecoverPersistedSession();

            // Assert
            _fixture.Logger.Entries.Should().Contain(e => e.Level == SentryLevel.Debug);
        }

        [Fact]
        public void TryGetPersistentInstallationId_DirectoryNotFoundException_LogDebug()
        {
            // Arrange
            var sut = _fixture.GetSut();
            sut = new GlobalSessionManager(
                _fixture.Options,
                _fixture.Client,
                Substitute.For<IInternalScopeManager>(),
                persistedSessionProvider: _ => throw new DirectoryNotFoundException());

            // Act
            sut.TryRecoverPersistedSession();

            // Assert
            _fixture.Logger.Entries.Should().Contain(e => e.Level == SentryLevel.Debug);
        }

        [Fact]
        public void TryGetPersistentInstallationId_EndOfStreamException_LogError()
        {
            // Arrange
            var sut = _fixture.GetSut();
            sut = new GlobalSessionManager(
                _fixture.Options,
                _fixture.Client,
                Substitute.For<IInternalScopeManager>(),
                persistedSessionProvider: _ => throw new EndOfStreamException());

            // Act
            sut.TryRecoverPersistedSession();

            // Assert
            _fixture.Logger.Entries.Should().Contain(e => e.Level == SentryLevel.Error);
        }
        /*
        [Fact]
        public void TryGetPersistentInstallationId_SessionStarted_ReturnsLastSession()
        {
            // Arrange
            var sut = _fixture.GetSut();

            var sessionUpdate = sut.StartSession();

            // Act
            var persistedSessionUpdate = sut.TryRecoverPersistedSession();

            // Assert
            sessionUpdate.Should().NotBeNull();
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
            persistedSessionUpdate!.IsInitial.Should().BeFalse();
            persistedSessionUpdate!.Timestamp.Should().BeAfter(sessionUpdate!.Timestamp);
            persistedSessionUpdate!.Duration.Should().BeGreaterThan(sessionUpdate!.Duration);
            persistedSessionUpdate!.SequenceNumber.Should().Be(sessionUpdate!.SequenceNumber + 1);
        }
        */

        [Fact]
        public void TryGetPersistentInstallationId_SessionStarted_DidCrashDelegateNotProvided_EndsAsAbnormal()
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
        public void TryGetPersistentInstallationId_SessionStarted_CrashDelegateReturnsFalse_EndsAsAbnormal()
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
        public void TryGetPersistentInstallationId_SessionStarted_CrashDelegateReturnsTrue_EndsAsCrashed()
        {
            // Arrange
            _fixture.Options.CrashedLastRun = () => true;
            var sut = _fixture.GetSut();

            using var fixture = new Fixture(o =>
                o.CrashedLastRun = () => true);

            sut.StartSession();

            // Act
            var persistedSessionUpdate = sut.TryRecoverPersistedSession();

            // Assert
            persistedSessionUpdate.Should().NotBeNull();
            persistedSessionUpdate!.EndStatus.Should().Be(SessionEndStatus.Crashed);
        }

        [Fact]
        public void TryGetPersistentInstallationId_SessionPaused_EndsAsExited()
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
        public void TryGetPersistentInstallationId_SessionEnded_ReturnsNull()
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
    }
}
