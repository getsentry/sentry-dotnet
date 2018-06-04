using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Sentry.Tests.Helpers;
using Xunit;
using static Sentry.Internals.Constants;

namespace Sentry.Tests
{
    [Collection(DsnEnvironmentVariable)]
    public class SentryCoreTests : IDisposable
    {
        [Fact]
        public void IsEnabled_StartsOfFalse()
        {
            Assert.False(SentryCore.IsEnabled);
        }

        [Fact]
        public void Init_BrokenDsn_Throws()
        {
            Assert.Throws<UriFormatException>(() => SentryCore.Init("invalid stuff"));
        }

        [Fact]
        public void Init_ValidDsnWithSecret_EnablesSdk()
        {
            SentryCore.Init(DsnSamples.ValidDsnWithSecret);
            Assert.True(SentryCore.IsEnabled);
        }

        [Fact]
        public void Init_ValidDsnWithoutSecret_EnablesSdk()
        {
            SentryCore.Init(DsnSamples.ValidDsnWithoutSecret);
            Assert.True(SentryCore.IsEnabled);
        }

        [Fact]
        public void Init_DsnInstance_EnablesSdk()
        {
            var dsn = new Dsn(DsnSamples.ValidDsnWithoutSecret);
            SentryCore.Init(dsn);
            Assert.True(SentryCore.IsEnabled);
        }

        [Fact]
        public void Init_ValidDsnEnvironmentVariable_EnablesSdk()
        {
            EnvironmentVariableGuard.WithVariable(
                Sentry.Internals.Constants.DsnEnvironmentVariable,
                DsnSamples.ValidDsnWithSecret,
                () =>
                {
                    SentryCore.Init();
                    Assert.True(SentryCore.IsEnabled);
                });
        }

        [Fact]
        public void Init_InvalidDsnEnvironmentVariable_Throws()
        {
            EnvironmentVariableGuard.WithVariable(
                Sentry.Internals.Constants.DsnEnvironmentVariable,
                // If the variable was set, to non empty string but value is broken, better crash than silently disable
                DsnSamples.InvalidDsn,
                () =>
                {
                    var ex = Assert.Throws<ArgumentException>(() => SentryCore.Init());
                    Assert.Equal("Invalid DSN: A Project Id is required.", ex.Message);
                });
        }

        [Fact]
        public void Init_DisableDsnEnvironmentVariable_DisablesSdk()
        {
            EnvironmentVariableGuard.WithVariable(
                Sentry.Internals.Constants.DsnEnvironmentVariable,
                Sentry.Internals.Constants.DisableSdkDsnValue,
                () =>
                {
                    SentryCore.Init();
                    Assert.False(SentryCore.IsEnabled);
                });
        }

        [Fact]
        public void Init_EmptyDsn_DisabledSdk()
        {
            SentryCore.Init(string.Empty);
            Assert.False(SentryCore.IsEnabled);
        }

        [Fact]
        public void CloseAndFlush_MultipleCalls_NoOp()
        {
            SentryCore.CloseAndFlush();
            SentryCore.CloseAndFlush();
            Assert.False(SentryCore.IsEnabled);
        }

        [Fact]
        public void PushScope_InstanceOf_DisabledClient()
        {
            Assert.Same(Sentry.Internals.DisabledSentryClient.Instance, SentryCore.PushScope());
        }

        [Fact]
        public void PushScope_NullArgument_NoOp()
        {
            var scopeGuard = SentryCore.PushScope(null as object);
            Assert.False(SentryCore.IsEnabled);
            scopeGuard.Dispose();
        }

        [Fact]
        public void PushScope_Parameterless_NoOp()
        {
            var scopeGuard = SentryCore.PushScope();
            Assert.False(SentryCore.IsEnabled);
            scopeGuard.Dispose();
        }

        [Fact]
        public void PushScope_MultiCallState_SameDisposableInstance()
        {
            var state = new object();
            Assert.Same(SentryCore.PushScope(state), SentryCore.PushScope(state));
        }

        [Fact]
        public void PushScope_MultiCallParameterless_SameDisposableInstance() => Assert.Same(SentryCore.PushScope(), SentryCore.PushScope());

        [Fact]
        public void AddBreadcrumb_NoClock_NoOp() => SentryCore.AddBreadcrumb(message: null, type: null);

        [Fact]
        public void AddBreadcrumb_WithClock_NoOp() => SentryCore.AddBreadcrumb(clock: null, null, null);

        [Fact]
        public void ConfigureScope_Sync_CallbackNeverInvoked()
        {
            var invoked = false;
            SentryCore.ConfigureScope(_ => invoked = true);
            Assert.False(invoked);
        }

        [Fact]
        public async Task ConfigureScope_Async_CallbackNeverInvoked()
        {
            var invoked = false;
            await SentryCore.ConfigureScopeAsync(_ =>
            {
                invoked = true;
                return Task.CompletedTask;
            });
            Assert.False(invoked);
        }

        [Fact]
        public void CaptureEvent_Instance_NoOp() => SentryCore.CaptureEvent(new SentryEvent(null));

        [Fact]
        public void CaptureEvent_Func_NoOp()
        {
            var invoked = false;
            SentryCore.CaptureEvent(() =>
            {
                invoked = true;
                return null;
            });
            Assert.False(invoked);
        }

        [Fact]
        public void CaptureException_Instance_NoOp() => SentryCore.CaptureException(new Exception());

        [Fact]
        public void Implements_Client()
        {
            var clientMembers = typeof(ISentryClient).GetMembers(BindingFlags.Public | BindingFlags.Instance);
            var sentryCore = typeof(SentryCore).GetMembers(BindingFlags.Public | BindingFlags.Static);

            Assert.Empty(clientMembers.Select(m => m.ToString()).Except(sentryCore.Select(m => m.ToString())));
        }

        [Fact]
        public void Implements_ClientExtensions()
        {
            var clientExtensions = typeof(SentryClientExtensions).GetMembers(BindingFlags.Public | BindingFlags.Static)
                // Remove the extension argument: Method(this ISentryClient client, ...
                .Select(m => m.ToString().Replace($"({typeof(ISentryClient).FullName}, ", "("));
            var sentryCore = typeof(SentryCore).GetMembers(BindingFlags.Public | BindingFlags.Static);

            Assert.Empty(clientExtensions.Except(sentryCore.Select(m => m.ToString())));
        }

        [Fact]
        public void Implements_ScopeManagement()
        {
            var scopeManagement = typeof(ISentryScopeManagement).GetMembers(BindingFlags.Public | BindingFlags.Instance);
            var sentryCore = typeof(SentryCore).GetMembers(BindingFlags.Public | BindingFlags.Static);

            Assert.Empty(scopeManagement.Select(m => m.ToString()).Except(sentryCore.Select(m => m.ToString())));
        }

        public void Dispose()
        {
            SentryCore.CloseAndFlush();
        }
    }
}
