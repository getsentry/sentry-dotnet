﻿using System;
using System.IO;
using System.Linq;
using FluentAssertions;
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

            public GlobalSessionManager SessionManager { get; }

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

                configureOptions?.Invoke(Options);

                SessionManager = new GlobalSessionManager(Options);
            }

            public void Dispose() => _cacheDirectory.Dispose();
        }

        [Fact]
        public void StartSession_ReleaseSet_CreatesNewSession()
        {
            // Arrange
            using var fixture = new Fixture();

            // Act
            var sessionUpdate = fixture.SessionManager.StartSession();

            // Assert
            sessionUpdate.Should().NotBeNull();
            sessionUpdate?.Id.Should().NotBe(SentryId.Empty);
            sessionUpdate?.Release.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public void StartSession_CacheDirectoryProvided_InstallationIdFileCreated()
        {
            // Arrange
            using var fixture = new Fixture();

            var filePath = Path.Combine(
                fixture.Options.CacheDirectoryPath!,
                "Sentry",
                fixture.Options.Dsn!.GetHashString(),
                ".installation"
            );

            // Act
            fixture.SessionManager.StartSession();

            // Assert
            File.Exists(filePath).Should().BeTrue();
        }

        [Fact]
        public void StartSession_CacheDirectoryNotProvided_InstallationIdFileCreated()
        {
            // Arrange
            using var fixture = new Fixture(o =>
                o.CacheDirectoryPath = null
            );

            var filePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Sentry",
                fixture.Options.Dsn!.GetHashString(),
                ".installation"
            );

            // Act
            fixture.SessionManager.StartSession();

            // Assert
            File.Exists(filePath).Should().BeTrue();
        }

        [Fact]
        public void StartSession_InstallationId_AlwaysSameId()
        {
            // Arrange
            using var fixture = new Fixture();

            // Act
            var sessionUpdates = Enumerable
                .Range(0, 15)
                .Select(_ => fixture.SessionManager.StartSession())
                .ToArray();

            // Assert
            sessionUpdates.Select(s => s.DistinctId).Distinct().Should().ContainSingle();
        }

        [Fact]
        public void ReportError_ActiveSessionExists_ReturnsNewUpdateWithIncrementedErrorCount()
        {
            // Arrange
            using var fixture = new Fixture();

            fixture.SessionManager.StartSession();

            // Act
            var sessionUpdate = fixture.SessionManager.ReportError();

            // Assert
            sessionUpdate.Should().NotBeNull();
            sessionUpdate?.ErrorCount.Should().Be(1);
        }

        [Fact]
        public void ReportError_ActiveSessionExistsWithNonZeroErrorCount_DoesNotReturnNewUpdate()
        {
            // Arrange
            using var fixture = new Fixture();

            fixture.SessionManager.StartSession();

            // Act
            fixture.SessionManager.ReportError();
            var sessionUpdate = fixture.SessionManager.ReportError();

            // Assert
            sessionUpdate.Should().BeNull();
        }

        [Fact]
        public void ReportError_ActiveSessionDoesNotExist_LogsOutError()
        {
            // Arrange
            using var fixture = new Fixture();

            // Act
            fixture.SessionManager.ReportError();

            // Assert
            fixture.Logger.Entries.Should().Contain(e =>
                e.Message == "Failed to report an error on a session because there is none active." &&
                e.Level == SentryLevel.Debug
            );
        }

        [Fact]
        public void EndSession_ActiveSessionExists_EndsSession()
        {
            // Arrange
            using var fixture = new Fixture();

            fixture.SessionManager.StartSession();
            var session = fixture.SessionManager.CurrentSession;

            // Act
            var sessionUpdate = fixture.SessionManager.EndSession(SessionEndStatus.Exited);

            // Assert
            session.Should().NotBeNull();
            sessionUpdate?.EndStatus.Should().Be(SessionEndStatus.Exited);
        }

        [Fact]
        public void EndSession_ActiveSessionDoesNotExist_DoesNothing()
        {
            // Arrange
            using var fixture = new Fixture();

            // Act
            var endedSession = fixture.SessionManager.EndSession(SessionEndStatus.Exited);

            // Assert
            endedSession.Should().BeNull();

            fixture.Logger.Entries.Should().Contain(e =>
                e.Message == "Failed to end session because there is none active." &&
                e.Level == SentryLevel.Debug
            );
        }

        [Fact]
        public void GetMachineNameInstallationId_Hashed()
        {
            // Arrange
            using var fixture = new Fixture();

            // Act
            var installationId = fixture.SessionManager.GetMachineNameInstallationId();

            // Assert
            installationId.Should().NotBeNullOrWhiteSpace();
            installationId.Should().NotBeEquivalentTo(Environment.MachineName);
        }

        [Fact]
        public void GetMachineNameInstallationId_Idempotent()
        {
            // Arrange
            using var fixture = new Fixture();

            // Act
            var installationIds = Enumerable
                .Range(0, 10)
                .Select(_ => fixture.SessionManager.GetMachineNameInstallationId())
                .ToArray();

            // Assert
            installationIds.Distinct().Should().ContainSingle();
        }

        [Fact]
        public void TryGetPersistentInstallationId_SessionNotStarted_ReturnsNull()
        {
            // Arrange
            using var fixture = new Fixture();

            // Act
            var persistedSessionUpdate = fixture.SessionManager.TryRecoverPersistedSession();

            // Assert
            persistedSessionUpdate.Should().BeNull();
        }

        [Fact]
        public void TryGetPersistentInstallationId_SessionStarted_ReturnsLastSession()
        {
            // Arrange
            using var fixture = new Fixture();

            var sessionUpdate = fixture.SessionManager.StartSession();

            // Act
            var persistedSessionUpdate = fixture.SessionManager.TryRecoverPersistedSession();

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

        [Fact]
        public void TryGetPersistentInstallationId_SessionStarted_DidCrashDelegateNotProvided_EndsAsAbnormal()
        {
            // Arrange
            using var fixture = new Fixture();

            fixture.SessionManager.StartSession();

            // Act
            var persistedSessionUpdate = fixture.SessionManager.TryRecoverPersistedSession();

            // Assert
            persistedSessionUpdate.Should().NotBeNull();
            persistedSessionUpdate!.EndStatus.Should().Be(SessionEndStatus.Abnormal);
        }

        [Fact]
        public void TryGetPersistentInstallationId_SessionStarted_CrashDelegateReturnsFalse_EndsAsAbnormal()
        {
            // Arrange
            using var fixture = new Fixture(o =>
                o.CrashedLastRun = () => false
            );

            fixture.SessionManager.StartSession();

            // Act
            var persistedSessionUpdate = fixture.SessionManager.TryRecoverPersistedSession();

            // Assert
            persistedSessionUpdate.Should().NotBeNull();
            persistedSessionUpdate!.EndStatus.Should().Be(SessionEndStatus.Abnormal);
        }

        [Fact]
        public void TryGetPersistentInstallationId_SessionStarted_CrashDelegateReturnsTrue_EndsAsCrashed()
        {
            // Arrange
            using var fixture = new Fixture(o =>
                o.CrashedLastRun = () => true
            );

            fixture.SessionManager.StartSession();

            // Act
            var persistedSessionUpdate = fixture.SessionManager.TryRecoverPersistedSession();

            // Assert
            persistedSessionUpdate.Should().NotBeNull();
            persistedSessionUpdate!.EndStatus.Should().Be(SessionEndStatus.Crashed);
        }

        [Fact]
        public void TryGetPersistentInstallationId_SessionPaused_EndsAsExited()
        {
            // Arrange
            using var fixture = new Fixture();

            fixture.SessionManager.StartSession();
            fixture.SessionManager.PauseSession();

            // Act
            var persistedSessionUpdate = fixture.SessionManager.TryRecoverPersistedSession();

            // Assert
            persistedSessionUpdate.Should().NotBeNull();
            persistedSessionUpdate!.EndStatus.Should().Be(SessionEndStatus.Exited);
        }

        [Fact]
        public void TryGetPersistentInstallationId_SessionEnded_ReturnsNull()
        {
            // Arrange
            using var fixture = new Fixture();

            fixture.SessionManager.StartSession();
            fixture.SessionManager.EndSession(SessionEndStatus.Exited);

            // Act
            var persistedSessionUpdate = fixture.SessionManager.TryRecoverPersistedSession();

            // Assert
            persistedSessionUpdate.Should().BeNull();
        }
    }
}
