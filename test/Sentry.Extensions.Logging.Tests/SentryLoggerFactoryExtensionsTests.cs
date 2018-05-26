using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Sentry.Extensions.Logging.Tests
{
    public class SentryLoggerFactoryExtensionsTests
    {
        [Fact]
        public void AddSentry_WithOptionsCallback_CallbackInvoked()
        {
            var callbackInvoked = false;
            var expected = Substitute.For<ILoggerFactory>();
            expected.AddSentry(o => { callbackInvoked = true; });

            Assert.True(callbackInvoked);
        }

        [Fact]
        public void AddSentry_NoOptionsDelegate_ProviderAdded()
        {
            var expected = Substitute.For<ILoggerFactory>();
            expected.AddSentry();

            expected.Received(1)
                .AddProvider(Arg.Is<ILoggerProvider>(p => p is SentryLoggerProvider));
        }

        [Fact]
        public void AddSentry_ReturnsSameFactory()
        {
            var expected = Substitute.For<ILoggerFactory>();
            var actual = expected.AddSentry();

            Assert.Same(expected, actual);
        }

        [Fact]
        public void Namespace_MicrosoftExtensionsLogging()
        {
            var @namespace = typeof(SentryLoggerFactoryExtensions).Namespace;

            Assert.Equal("Microsoft.Extensions.Logging", @namespace);
        }
    }
}
