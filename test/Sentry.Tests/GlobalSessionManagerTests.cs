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
            var session = sessionManager.StartSession();

            // Assert
            session.Should().NotBeNull();
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

            var previousSession = sessionManager.StartSession();

            // Act
            var session = sessionManager.StartSession();

            // Assert
            session.Should().NotBe(previousSession);
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

            var filePath = Path.Combine(tempDirectory.Path, ".installation");

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
            var sessions = Enumerable.Range(0, 15).Select(_ => sessionManager.StartSession()).ToArray();

            // Assert
            sessions.Select(s => s.DistinctId).Distinct().Should().ContainSingle();
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

            var session = sessionManager.StartSession();

            // Act
            sessionManager.ReportError();
            sessionManager.ReportError();
            sessionManager.ReportError();

            // Assert
            session.Should().NotBeNull();
            session?.ErrorCount.Should().Be(3);
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
                e.Level == SentryLevel.Error
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

            var session = sessionManager.StartSession();

            // Act
            var endedSession = sessionManager.EndSession(SessionEndStatus.Exited);

            // Assert
            session.Should().NotBeNull();
            session.Should().Be(endedSession);
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
