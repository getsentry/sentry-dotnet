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
            var sessionManager = new GlobalSessionManager(new SentryOptions
            {
                Release = "foo"
            });

            // Act
            var session = sessionManager.StartSession();

            // Assert
            session.Should().NotBeNull();
            session?.Release.Should().Be("foo");
        }

        [Fact]
        public void StartSession_ReleaseUnset_DoesNotCreateSession()
        {
            // Arrange
            var sessionManager = new GlobalSessionManager(new SentryOptions());

            // Act
            var session = sessionManager.StartSession();

            // Assert
            session.Should().BeNull();
        }

        [Fact]
        public void StartSession_ActiveSessionExists_EndsPreviousSession()
        {
            // Arrange
            var sessionManager = new GlobalSessionManager(new SentryOptions
            {
                Release = "foo"
            });

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
            var sessionManager = new GlobalSessionManager(new SentryOptions
            {
                Release = "foo"
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
            var sessionManager = new GlobalSessionManager(new SentryOptions
            {
                Release = "foo"
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
        public void ReportError_ActiveSessionDoesNotExist_IncrementsErrorCount()
        {
            // Arrange
            var logger = new InMemoryDiagnosticLogger();

            var sessionManager = new GlobalSessionManager(new SentryOptions
            {
                Release = "foo",
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
    }
}
