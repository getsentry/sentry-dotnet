using Microsoft.Extensions.Logging;
using NSubstitute;
using Sentry.Extensibility;
using Xunit;

namespace Sentry.Extensions.Logging.Tests
{
    public class SentryLoggerFactoryExtensionsTests
    {
        [Fact]
        public void AddSentry_NoDiagnosticSet_MelSet()
        {
            SentryLoggingOptions options = null;
            var sut = Substitute.For<ILoggerFactory>();
            sut.AddSentry(o =>
            {
                o.Debug = true;
                options = o;
            });

            Assert.IsType<MelDiagnosticLogger>(options.DiagnosticLogger);
        }

        [Fact]
        public void AddSentry_DiagnosticSet_NoOverriden()
        {
            SentryLoggingOptions options = null;
            var sut = Substitute.For<ILoggerFactory>();
            var diagnosticLogger = Substitute.For<IDiagnosticLogger>();
            sut.AddSentry(o =>
            {
                o.Debug = true;
                Assert.Null(o.DiagnosticLogger);
                o.DiagnosticLogger = diagnosticLogger;
                options = o;
            });

            Assert.Same(diagnosticLogger, options.DiagnosticLogger);
        }

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
        public void AddSentry_ConfigureOptionsOverload_ReturnsSameFactory()
        {
            var expected = Substitute.For<ILoggerFactory>();
            var actual = expected.AddSentry(_ => { });

            Assert.Same(expected, actual);
        }

        [Fact]
        public void AddSentry_ConfigureOptionsOverload_InvokesCallback()
        {
            var expected = Substitute.For<ILoggerFactory>();

            var invoked = false;
            expected.AddSentry(o =>
            {
                Assert.NotNull(o);
                invoked = true;
            });

            Assert.True(invoked);
        }

        [Fact]
        public void Namespace_MicrosoftExtensionsLogging()
        {
            var @namespace = typeof(SentryLoggerFactoryExtensions).Namespace;

            Assert.Equal("Microsoft.Extensions.Logging", @namespace);
        }
    }
}
