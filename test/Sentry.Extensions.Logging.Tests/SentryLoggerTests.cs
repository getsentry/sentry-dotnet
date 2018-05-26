using Microsoft.Extensions.Logging;
using NSubstitute;
using Sentry.Extensibility;
using Sentry.Infrastructure;
using Xunit;

namespace Sentry.Extensions.Logging.Tests
{
    public class SentryLoggerTests
    {
        private class Fixture
        {
            public string CategoryName { get; set; } = nameof(SentryLoggerTests);
            public ISystemClock Clock { get; set; } = Substitute.For<ISystemClock>();
            public ISdk Sdk { get; set; } = Substitute.For<ISdk>();
            public SentryLoggingOptions Options { get; set; } = new SentryLoggingOptions();

            public SentryLogger GetSut() => new SentryLogger(CategoryName, Options, Clock, Sdk);
        }

        private readonly Fixture _fixture = new Fixture();

        [Fact]
        public void IsEnabled_DisabledSdk_ReturnsFalse()
        {
            _fixture.Sdk.IsEnabled.Returns(false);

            var sut = _fixture.GetSut();

            Assert.False(sut.IsEnabled(LogLevel.Critical));
        }
    }
}
