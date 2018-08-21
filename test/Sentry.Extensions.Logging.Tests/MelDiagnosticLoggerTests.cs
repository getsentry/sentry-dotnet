using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Internal;
using NSubstitute;
using Sentry.Protocol;
using Xunit;

namespace Sentry.Extensions.Logging.Tests
{
    public class MelDiagnosticLoggerTests
    {
        private class Fixture
        {
            public ILogger<ISentryClient> MelLogger { get; set; } = Substitute.For<ILogger<ISentryClient>>();
            public SentryLevel Level { get; set; } = SentryLevel.Warning;

            public MelDiagnosticLogger GetSut() => new MelDiagnosticLogger(MelLogger, Level);
        }

        private readonly Fixture _fixture = new Fixture();

        [Fact]
        public void LogLevel_ErrorLevel_IsEnabledTrue()
        {
            var sut = _fixture.GetSut();
            _fixture.MelLogger.IsEnabled(LogLevel.Error).Returns(true);
            Assert.True(sut.IsEnabled(SentryLevel.Error));
        }

        [Fact]
        public void LogLevel_InfoLevel_IsEnabledFalse()
        {
            var sut = _fixture.GetSut();
            _fixture.MelLogger.IsEnabled(LogLevel.Information).Returns(true);
            Assert.False(sut.IsEnabled(SentryLevel.Info));
        }

        [Fact]
        public void LogLevel_HigherLevel_IsEnabled()
        {
            _fixture.MelLogger.IsEnabled(LogLevel.Debug).Returns(true);
            var sut = _fixture.GetSut();
            Assert.False(sut.IsEnabled(SentryLevel.Info));
        }

        [Fact]
        public void Log_PassedThrough()
        {
            const SentryLevel expectedLevel = SentryLevel.Debug;
            const string expectedMessage = "test";
            var expectedException = new Exception();

            _fixture.Level = SentryLevel.Debug;
            var sut = _fixture.GetSut();
            _fixture.MelLogger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

            sut.Log(expectedLevel, expectedMessage, expectedException);

            _fixture.MelLogger.Received(1).Log<object>(
                expectedLevel.ToMicrosoft(),
                0,
                Arg.Is<FormattedLogValues>(e => e.ToString() == expectedMessage),
                expectedException,
                Arg.Any<Func<object, Exception, string>>());
        }
    }
}
