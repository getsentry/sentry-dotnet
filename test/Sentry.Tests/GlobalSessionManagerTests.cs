using System;
using System.IO;
using System.Linq;
using FluentAssertions;
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

            public Fixture()
            {
                Logger = new InMemoryDiagnosticLogger();

                Options = new SentryOptions
                {
                    CacheDirectoryPath = _cacheDirectory.Path,
                    Release = "test",
                    Debug = true,
                    DiagnosticLogger = Logger
                };

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
            sessionUpdate?.Id.Should().NotBeNullOrWhiteSpace();
            sessionUpdate?.Release.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public void StartSession_ActiveSessionExists_EndsPreviousSession()
        {
            // Arrange
            using var fixture = new Fixture();

            fixture.SessionManager.StartSession();
            var previousSession = fixture.SessionManager.CurrentSession;

            // Act
            fixture.SessionManager.StartSession();
            var session = fixture.SessionManager.CurrentSession;

            // Assert
            session.Should().NotBe(previousSession);
            session?.Id.Should().NotBe(previousSession?.Id);
            previousSession?.EndStatus.Should().Be(SessionEndStatus.Exited);
        }

        [Fact]
        public void StartSession_CacheDirectoryProvided_InstallationIdFileCreated()
        {
            // Arrange
            using var fixture = new Fixture();

            var filePath = Path.Combine(fixture.Options.CacheDirectoryPath!, "Sentry", ".installation");

            // Act
            fixture.SessionManager.StartSession();

            // Assert
            File.Exists(filePath).Should().BeTrue();
        }

        [Fact]
        public void StartSession_CacheDirectoryNotProvided_InstallationIdFileCreated()
        {
            // Arrange
            using var fixture = new Fixture();
            fixture.Options.CacheDirectoryPath = null;

            var filePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Sentry",
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
        public void ReportError_ActiveSessionExists_IncrementsErrorCount()
        {
            // Arrange
            using var fixture = new Fixture();

            fixture.SessionManager.StartSession();

            // Act
            fixture.SessionManager.ReportError();
            fixture.SessionManager.ReportError();
            var sessionUpdate = fixture.SessionManager.ReportError();

            // Assert
            sessionUpdate.Should().NotBeNull();
            sessionUpdate?.ErrorCount.Should().Be(3);
        }

        [Fact]
        public void ReportError_ActiveSessionDoesNotExist_LogsOutError()
        {
            // Arrange
            using var fixture = new Fixture();

            // Act
            fixture.SessionManager.ReportError();
            fixture.SessionManager.ReportError();
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
            fixture.SessionManager.EndSession(SessionEndStatus.Exited);

            // Assert
            session.Should().NotBeNull();
            session?.EndStatus.Should().Be(SessionEndStatus.Exited);
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
                e.Level == SentryLevel.Error
            );
        }
    }
}
