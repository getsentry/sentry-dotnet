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
        [Fact]
        public void StartSession_ReleaseSet_CreatesNewSession()
        {
            // Arrange
            using var tempDirectory = new TempDirectory();
            var sessionManager = new GlobalSessionManager(new SentryOptions
            {
                CacheDirectoryPath = tempDirectory.Path,
                Release = "test"
            });

            // Act
            var sessionUpdate = sessionManager.StartSession();

            // Assert
            sessionUpdate.Should().NotBeNull();
            sessionUpdate?.Id.Should().NotBeNullOrWhiteSpace();
            sessionUpdate?.Release.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public void StartSession_ActiveSessionExists_EndsPreviousSession()
        {
            // Arrange
            using var tempDirectory = new TempDirectory();
            var sessionManager = new GlobalSessionManager(new SentryOptions
            {
                CacheDirectoryPath = tempDirectory.Path,
                Release = "test"
            });

            sessionManager.StartSession();
            var previousSession = sessionManager.CurrentSession;

            // Act
            sessionManager.StartSession();
            var session = sessionManager.CurrentSession;

            // Assert
            session.Should().NotBe(previousSession);
            session?.Id.Should().NotBe(previousSession?.Id);
            previousSession?.EndStatus.Should().Be(SessionEndStatus.Exited);
        }

        [Fact]
        public void StartSession_CacheDirectoryProvided_InstallationIdFileCreated()
        {
            // Arrange
            using var tempDirectory = new TempDirectory();
            var sessionManager = new GlobalSessionManager(new SentryOptions
            {
                CacheDirectoryPath = tempDirectory.Path,
                Release = "test"
            });

            var filePath = Path.Combine(tempDirectory.Path, "Sentry", ".installation");

            // Act
            sessionManager.StartSession();

            // Assert
            File.Exists(filePath).Should().BeTrue();
        }

        [Fact]
        public void StartSession_CacheDirectoryNotProvided_InstallationIdFileCreated()
        {
            // Arrange
            using var tempDirectory = new TempDirectory();
            var sessionManager = new GlobalSessionManager(new SentryOptions
            {
                Release = "test"
            });

            var filePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Sentry",
                ".installation"
            );

            // Act
            sessionManager.StartSession();

            // Assert
            File.Exists(filePath).Should().BeTrue();
        }

        [Fact]
        public void StartSession_InstallationId_AlwaysSameId()
        {
            // Arrange
            using var tempDirectory = new TempDirectory();
            var sessionManager = new GlobalSessionManager(new SentryOptions
            {
                CacheDirectoryPath = tempDirectory.Path,
                Release = "test"
            });

            // Act
            var sessionUpdates = Enumerable.Range(0, 15).Select(_ => sessionManager.StartSession()).ToArray();

            // Assert
            sessionUpdates.Select(s => s.DistinctId).Distinct().Should().ContainSingle();
        }

        [Fact]
        public void ReportError_ActiveSessionExists_IncrementsErrorCount()
        {
            // Arrange
            using var tempDirectory = new TempDirectory();
            var sessionManager = new GlobalSessionManager(new SentryOptions
            {
                CacheDirectoryPath = tempDirectory.Path,
                Release = "test"
            });

            sessionManager.StartSession();

            // Act
            sessionManager.ReportError();
            sessionManager.ReportError();
            var sessionUpdate = sessionManager.ReportError();

            // Assert
            sessionUpdate.Should().NotBeNull();
            sessionUpdate?.ErrorCount.Should().Be(3);
        }

        [Fact]
        public void ReportError_ActiveSessionDoesNotExist_LogsOutError()
        {
            // Arrange
            using var tempDirectory = new TempDirectory();

            var logger = new InMemoryDiagnosticLogger();

            var sessionManager = new GlobalSessionManager(new SentryOptions
            {
                CacheDirectoryPath = tempDirectory.Path,
                Release = "test",
                DiagnosticLogger = logger,
                Debug = true
            });

            // Act
            sessionManager.ReportError();
            sessionManager.ReportError();
            sessionManager.ReportError();

            // Assert
            logger.Entries.Should().Contain(e =>
                e.Message == "Failed to report an error on a session because there is none active." &&
                e.Level == SentryLevel.Debug
            );
        }

        [Fact]
        public void EndSession_ActiveSessionExists_EndsSession()
        {
            // Arrange
            using var tempDirectory = new TempDirectory();
            var sessionManager = new GlobalSessionManager(new SentryOptions
            {
                CacheDirectoryPath = tempDirectory.Path,
                Release = "test"
            });

            sessionManager.StartSession();
            var session = sessionManager.CurrentSession;

            // Act
            sessionManager.EndSession(SessionEndStatus.Exited);

            // Assert
            session.Should().NotBeNull();
            session?.EndStatus.Should().Be(SessionEndStatus.Exited);
        }

        [Fact]
        public void EndSession_ActiveSessionDoesNotExist_DoesNothing()
        {
            // Arrange
            using var tempDirectory = new TempDirectory();

            var logger = new InMemoryDiagnosticLogger();

            var sessionManager = new GlobalSessionManager(new SentryOptions
            {
                CacheDirectoryPath = tempDirectory.Path,
                Release = "test",
                DiagnosticLogger = logger,
                Debug = true
            });

            // Act
            var endedSession = sessionManager.EndSession(SessionEndStatus.Exited);

            // Assert
            endedSession.Should().BeNull();

            logger.Entries.Should().Contain(e =>
                e.Message == "Failed to end session because there is none active." &&
                e.Level == SentryLevel.Error
            );
        }
    }
}
