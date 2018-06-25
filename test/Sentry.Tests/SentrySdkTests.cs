using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Sentry.Testing;
using Sentry.Tests.Helpers;
using Xunit;
using static Sentry.Internal.Constants;
using static Sentry.DsnSamples;

namespace Sentry.Tests
{
    [Collection(nameof(SentrySdkCollection))]
    public class SentrySdkTests : SentrySdkTestFixture
    {
        [Fact]
        public void IsEnabled_StartsOfFalse()
        {
            Assert.False(SentrySdk.IsEnabled);
        }

        [Fact]
        public void Init_BrokenDsn_Throws()
        {
            Assert.Throws<UriFormatException>(() => SentrySdk.Init("invalid stuff"));
        }

        [Fact]
        public void Init_ValidDsnWithSecret_EnablesSdk()
        {
            using (SentrySdk.Init(ValidDsnWithSecret))
                Assert.True(SentrySdk.IsEnabled);
        }

        [Fact]
        public void Init_ValidDsnWithoutSecret_EnablesSdk()
        {
            using (SentrySdk.Init(DsnSamples.ValidDsnWithoutSecret))
                Assert.True(SentrySdk.IsEnabled);
        }

        [Fact]
        public void Init_DsnInstance_EnablesSdk()
        {
            var dsn = new Dsn(DsnSamples.ValidDsnWithoutSecret);
            using (SentrySdk.Init(dsn))
                Assert.True(SentrySdk.IsEnabled);
        }

        [Fact]
        public void Init_ValidDsnEnvironmentVariable_EnablesSdk()
        {
            EnvironmentVariableGuard.WithVariable(
                DsnEnvironmentVariable,
                ValidDsnWithSecret,
                () =>
                {
                    using (SentrySdk.Init())
                        Assert.True(SentrySdk.IsEnabled);
                });
        }

        [Fact]
        public void Init_InvalidDsnEnvironmentVariable_Throws()
        {
            EnvironmentVariableGuard.WithVariable(
                DsnEnvironmentVariable,
                // If the variable was set, to non empty string but value is broken, better crash than silently disable
                DsnSamples.InvalidDsn,
                () =>
                {
                    var ex = Assert.Throws<ArgumentException>(() => SentrySdk.Init());
                    Assert.Equal("Invalid DSN: A Project Id is required.", ex.Message);
                });
        }

        [Fact]
        public void Init_DisableDsnEnvironmentVariable_DisablesSdk()
        {
            EnvironmentVariableGuard.WithVariable(
                DsnEnvironmentVariable,
                DisableSdkDsnValue,
                () =>
                {
                    using (SentrySdk.Init())
                        Assert.False(SentrySdk.IsEnabled);
                });
        }

        [Fact]
        public void Init_EmptyDsn_DisabledSdk()
        {
            using (SentrySdk.Init(string.Empty))
                Assert.False(SentrySdk.IsEnabled);
        }

        [Fact]
        public void Disposable_MultipleCalls_NoOp()
        {
            var disposable = SentrySdk.Init();
            disposable.Dispose();
            disposable.Dispose();
            Assert.False(SentrySdk.IsEnabled);
        }

        [Fact]
        public void Init_MultipleCalls_ReplacesHubWithLatest()
        {
            var first = SentrySdk.Init(ValidDsnWithSecret);
            SentrySdk.AddBreadcrumb("test", "category");
            var called = false;
            SentrySdk.ConfigureScope(p =>
            {
                called = true;
                Assert.Single(p.Breadcrumbs);
            });
            Assert.True(called);
            called = false;

            var second = SentrySdk.Init(ValidDsnWithSecret);
            SentrySdk.ConfigureScope(p =>
            {
                called = true;
                Assert.Empty(p.Breadcrumbs);
            });
            Assert.True(called);

            first.Dispose();
            second.Dispose();
        }

        [Fact]
        public void Dispose_DisposingFirst_DoesntAffectSecond()
        {
            var first = SentrySdk.Init(ValidDsnWithSecret);
            var second = SentrySdk.Init(ValidDsnWithSecret);
            SentrySdk.AddBreadcrumb("test", "category");
            first.Dispose();
            var called = false;
            SentrySdk.ConfigureScope(p =>
            {
                called = true;
                Assert.Single(p.Breadcrumbs);
            });
            Assert.True(called);
            second.Dispose();
        }

        [Fact]
        public void PushScope_InstanceOf_DisabledClient()
        {
            Assert.Same(DisabledHub.Instance, SentrySdk.PushScope());
        }

        [Fact]
        public void PushScope_NullArgument_NoOp()
        {
            var scopeGuard = SentrySdk.PushScope(null as object);
            Assert.False(SentrySdk.IsEnabled);
            scopeGuard.Dispose();
        }

        [Fact]
        public void PushScope_Parameterless_NoOp()
        {
            var scopeGuard = SentrySdk.PushScope();
            Assert.False(SentrySdk.IsEnabled);
            scopeGuard.Dispose();
        }

        [Fact]
        public void PushScope_MultiCallState_SameDisposableInstance()
        {
            var state = new object();
            Assert.Same(SentrySdk.PushScope(state), SentrySdk.PushScope(state));
        }

        [Fact]
        public void PushScope_MultiCallParameterless_SameDisposableInstance() => Assert.Same(SentrySdk.PushScope(), SentrySdk.PushScope());

        [Fact]
        public void AddBreadcrumb_NoClock_NoOp() => SentrySdk.AddBreadcrumb(message: null);

        [Fact]
        public void AddBreadcrumb_WithClock_NoOp() => SentrySdk.AddBreadcrumb(clock: null, null);

        [Fact]
        public void ConfigureScope_Sync_CallbackNeverInvoked()
        {
            var invoked = false;
            SentrySdk.ConfigureScope(_ => invoked = true);
            Assert.False(invoked);
        }

        [Fact]
        public async Task ConfigureScope_Async_CallbackNeverInvoked()
        {
            var invoked = false;
            await SentrySdk.ConfigureScopeAsync(_ =>
            {
                invoked = true;
                return Task.CompletedTask;
            });
            Assert.False(invoked);
        }

        [Fact]
        public void CaptureEvent_Instance_NoOp() => SentrySdk.CaptureEvent(new SentryEvent());

        [Fact]
        public void CaptureException_Instance_NoOp() => SentrySdk.CaptureException(new Exception());

        [Fact]
        public void Implements_Client()
        {
            var clientMembers = typeof(ISentryClient).GetMembers(BindingFlags.Public | BindingFlags.Instance);
            var SentrySdk = typeof(SentrySdk).GetMembers(BindingFlags.Public | BindingFlags.Static);

            Assert.Empty(clientMembers.Select(m => m.ToString()).Except(SentrySdk.Select(m => m.ToString())));
        }

        [Fact]
        public void Implements_ClientExtensions()
        {
            var clientExtensions = typeof(SentryClientExtensions).GetMembers(BindingFlags.Public | BindingFlags.Static)
                // Remove the extension argument: Method(this ISentryClient client, ...
                .Select(m => m.ToString().Replace($"({typeof(ISentryClient).FullName}, ", "("));
            var SentrySdk = typeof(SentrySdk).GetMembers(BindingFlags.Public | BindingFlags.Static);

            Assert.Empty(clientExtensions.Except(SentrySdk.Select(m => m.ToString())));
        }

        [Fact]
        public void Implements_ScopeManagement()
        {
            var scopeManagement = typeof(ISentryScopeManager).GetMembers(BindingFlags.Public | BindingFlags.Instance);
            var SentrySdk = typeof(SentrySdk).GetMembers(BindingFlags.Public | BindingFlags.Static);

            Assert.Empty(scopeManagement.Select(m => m.ToString()).Except(SentrySdk.Select(m => m.ToString())));
        }
    }
}
