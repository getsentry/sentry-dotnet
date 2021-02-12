#if NETCOREAPP2_1 || NET461
using System;
#endif
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Sentry.Extensions.Logging.Tests
{
    public class MelDiagnosticLoggerTests
    {
        private class Fixture
        {
            public ILogger<ISentryClient> MelLogger { get; set; } = Substitute.For<ILogger<ISentryClient>>();
            public SentryLevel Level { get; set; } = SentryLevel.Warning;

            public MelDiagnosticLogger GetSut() => new(MelLogger, Level);
        }

        private readonly Fixture _fixture = new();

        [Fact]
        public void LogLevel_ErrorLevel_IsEnabledTrue()
        {
            var sut = _fixture.GetSut();
            _ = _fixture.MelLogger.IsEnabled(LogLevel.Error).Returns(true);
            Assert.True(sut.IsEnabled(SentryLevel.Error));
        }

        [Fact]
        public void LogLevel_InfoLevel_IsEnabledFalse()
        {
            var sut = _fixture.GetSut();
            _ = _fixture.MelLogger.IsEnabled(LogLevel.Information).Returns(true);
            Assert.False(sut.IsEnabled(SentryLevel.Info));
        }

        [Fact]
        public void LogLevel_HigherLevel_IsEnabled()
        {
            _ = _fixture.MelLogger.IsEnabled(LogLevel.Debug).Returns(true);
            var sut = _fixture.GetSut();
            Assert.False(sut.IsEnabled(SentryLevel.Info));
        }

        // .NET Core 3 (and hence .NET 5) has turned FormattedLogValues into an internal readonly struct
        // and now we can't match that with NSubstitute
#if NETCOREAPP2_1 || NET461
        [Fact]
        public void Log_PassedThrough()
        {
            const SentryLevel expectedLevel = SentryLevel.Debug;
            const string expectedMessage = "test";
            var expectedException = new Exception();

            _fixture.Level = SentryLevel.Debug;
            var sut = _fixture.GetSut();
            _ = _fixture.MelLogger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

            sut.Log(expectedLevel, expectedMessage, expectedException);

            _fixture.MelLogger.Received(1).Log(
                expectedLevel.ToMicrosoft(),
                0,
                Arg.Is<object>(e => e.ToString() == expectedMessage),
                expectedException,
                Arg.Any<Func<object, Exception, string>>());
        }
#endif
    }
}
