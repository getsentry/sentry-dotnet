using System;
using FluentAssertions;
using NSubstitute;
using Sentry.Infrastructure;
using Sentry.Testing;
using Sentry.Tests.Helpers;
using Xunit;

namespace Sentry.Tests
{
    public class ServerSessionManagerTests
    {
        private class Fixture : IDisposable
        {
            private readonly TempDirectory _cacheDirectory = new();

            public InMemoryDiagnosticLogger Logger { get; }

            public SentryOptions Options { get; }

            public ISentryClient Client { get; }

            public ISystemClock Clock { get; } = new SystemClock();

            public const string Release = "testrel";
            public const string Environment = "testenv";

            public Func<string, PersistedSessionUpdate> PersistedSessionProvider { get; }

            public Fixture(Action<SentryOptions> configureOptions = null)
            {
                Logger = new InMemoryDiagnosticLogger();

                Options = new SentryOptions
                {
                    Dsn = DsnSamples.ValidDsnWithoutSecret,
                    CacheDirectoryPath = _cacheDirectory.Path,
                    Release = Release,
                    Environment = Environment,
                    Debug = true,
                    DiagnosticLogger = Logger
                };

                Client = Substitute.For<ISentryClient>();

                configureOptions?.Invoke(Options);
            }

            public ServerSessionManager GetSut() =>
                new(
                    Options,
                    Client,
                    Clock);

            public void Dispose() => _cacheDirectory.Dispose();

        }

        private readonly Fixture _fixture = new();


        [Fact]
        public void StartSession_CountersNotIncreased()
        {
            // Arrange
            var sut = _fixture.GetSut();

            // Act
            sut.StartSession();

            // Assert
            Assert.Equal(0, sut.ErroredCount);
            Assert.Equal(0, sut.ExitedCount);
        }
        [Fact]
        public void EndSession_ExitedState_ExitCountIncreased()
        {
            // Arrange
            var sut = _fixture.GetSut();

            // Act
            sut.EndSession(SessionEndStatus.Exited);

            // Assert
            Assert.Equal(1, sut.ExitedCount);
        }

        [Fact]
        public void EndSession_ErroredState_ErrorCountIncreased()
        {
            // Arrange
            var sut = _fixture.GetSut();

            // Act
            sut.EndSession(SessionEndStatus.Abnormal);

            // Assert
            Assert.Equal(1, sut.ErroredCount);
        }

        [Fact]
        public void Flush_NoSessionsCounted_DoesNothing()
        {
            // Arrange
            var sut = _fixture.GetSut();

            // Act
            sut.Flush();

            // Assert
            _fixture.Client.Received(0).CaptureSessionAggregate(Arg.Any<SessionAggregate>());
            _fixture.Logger.Entries.Should().Contain(e =>
                e.Message == "No sessions to aggregate." &&
                e.Level == SentryLevel.Debug
            );
        }

        [Theory]
        [InlineData(SessionEndStatus.Abnormal)]
        [InlineData(SessionEndStatus.Crashed)]
        [InlineData(SessionEndStatus.Exited)]
        public void Flush_HasSessionsCounted_ResetCounters(SessionEndStatus status)
        {
            // Arrange
            var sut = _fixture.GetSut();
            sut.EndSession(status);

            // Act
            sut.Flush();

            // Assert
            Assert.Equal(0, sut.ErroredCount);
            Assert.Equal(0, sut.ExitedCount);
        }

        [Theory]
        [InlineData(SessionEndStatus.Abnormal)]
        [InlineData(SessionEndStatus.Crashed)]
        [InlineData(SessionEndStatus.Exited)]
        public void Flush_HasSessionsCounted_SessionCaptured(SessionEndStatus status)
        {
            // Arrange
            var sut = _fixture.GetSut();
            sut.EndSession(status);

            // Act
            sut.Flush();

            // Assert
            _fixture.Client.Received(1).CaptureSessionAggregate(Arg.Is<SessionAggregate>(aggregate => aggregate.HasCountForStatus(status)));
            _fixture.Client.Received(1).CaptureSessionAggregate(Arg.Is<SessionAggregate>(session => Fixture.Environment == session.Environment));
            _fixture.Client.Received(1).CaptureSessionAggregate(Arg.Is<SessionAggregate>(session => Fixture.Release == session.Release));
            _fixture.Logger.Entries.Should().Contain(e =>
                e.Message == "Flushed a session aggregate." &&
                e.Level == SentryLevel.Info
            );
        }
    }
}
