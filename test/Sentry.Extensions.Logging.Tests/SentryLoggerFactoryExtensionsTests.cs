using System;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Protocol;
using Xunit;

namespace Sentry.Extensions.Logging.Tests
{
    public class SentryLoggerFactoryExtensionsTests
    {
        [Fact]
        public void AddSentry_ConfigureScope_HubEnabledTrue_InvokesCallback()
        {
            const SentryLevel expected = SentryLevel.Debug;
            var sut = Substitute.For<ILoggerFactory>();
            var hub = Substitute.For<IHub>();
            hub.IsEnabled.Returns(true);
            var scope = new Scope(new SentryOptions());
            hub.When(w => w.ConfigureScope(Arg.Any<Action<Scope>>()))
                .Do(info => info.Arg<Action<Scope>>()(scope));
            SentrySdk.UseHub(hub);

            sut.AddSentry(o =>
            {
                o.InitializeSdk = false; // use the mock above
                o.ConfigureScope(s => s.Level = expected);
            });

            Assert.Equal(expected, scope.Level);
        }

        [Fact]
        public void AddSentry_ConfigureScope_HubEnabledFalse_DoesNotInvokesCallback()
        {
            const SentryLevel expected = SentryLevel.Debug;
            var sut = Substitute.For<ILoggerFactory>();
            var hub = Substitute.For<IHub>();
            hub.IsEnabled.Returns(false);
            var scope = new Scope(new SentryOptions());
            hub.When(w => w.ConfigureScope(Arg.Any<Action<Scope>>()))
                .Do(info => info.Arg<Action<Scope>>()(scope));
            SentrySdk.UseHub(hub);

            sut.AddSentry(o =>
            {
                o.InitializeSdk = false; // use the mock above
                o.ConfigureScope(s => s.Level = expected);
            });

            Assert.NotEqual(expected, scope.Level);
        }

        [Fact]
        public void AddSentry_InitializeSdkFalse_HubAdapter()
        {
            var sut = Substitute.For<ILoggerFactory>();

            sut.AddSentry(o => o.InitializeSdk = false);

            sut.Received(1)
                .AddProvider(Arg.Is<SentryLoggerProvider>(p => p.Hub == HubAdapter.Instance));
        }

        [Fact]
        public void AddSentry_DefaultOptions_InstantiateOptionalHub()
        {
            var sut = Substitute.For<ILoggerFactory>();

            sut.AddSentry();

            sut.Received(1)
                .AddProvider(Arg.Is<SentryLoggerProvider>(p => p.Hub is OptionalHub));
        }

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
