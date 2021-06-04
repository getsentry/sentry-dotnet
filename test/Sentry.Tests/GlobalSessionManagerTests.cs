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
            var sessionManager = new GlobalSessionManager(new SentryOptions());

            // Act
            var session = sessionManager.StartSession();

            // Assert
            session.Should().NotBeNull();
        }

        [Fact]
        public void StartSession_ActiveSessionExists_EndsPreviousSession()
        {
            // Arrange
            var sessionManager = new GlobalSessionManager(new SentryOptions());

            var previousSession = sessionManager.StartSession();

            // Act
            var session = sessionManager.StartSession();

            // Assert
            session.Should().NotBe(previousSession);
            previousSession?.EndStatus.Should().Be(SessionEndStatus.Exited);
        }

        [Fact]
        public void StartSession_InstallationId_SameId()
        {
            // Arrange
            var sessionManager = new GlobalSessionManager(new SentryOptions());

            // Act
            var sessions = Enumerable.Range(0, 15).Select(_ => sessionManager.StartSession()).ToArray();

            // Assert
            sessions.Select(s => s.DistinctId).Distinct().Should().ContainSingle();
        }

        [Fact]
        public void ReportError_ActiveSessionExists_IncrementsErrorCount()
        {
            // Arrange
            var sessionManager = new GlobalSessionManager(new SentryOptions());

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
        public void ReportError_ActiveSessionDoesNotExist_IncrementsErrorCount()
        {
            // Arrange
            var logger = new InMemoryDiagnosticLogger();

            var sessionManager = new GlobalSessionManager(new SentryOptions
            {
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
            var sessionManager = new GlobalSessionManager(new SentryOptions());

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
            var logger = new InMemoryDiagnosticLogger();

            var sessionManager = new GlobalSessionManager(new SentryOptions
            {
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
