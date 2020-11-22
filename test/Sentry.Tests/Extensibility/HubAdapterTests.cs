using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using NSubstitute;
using Sentry.Extensibility;
using Sentry.Infrastructure;
using Sentry.Protocol;
using Sentry.Testing;
using Xunit;

namespace Sentry.Tests.Extensibility
{
    [Collection(nameof(SentrySdkCollection))]
    public class HubAdapterTests : SentrySdkTestFixture
    {
        public IHub Hub { get; set; }

        public HubAdapterTests()
        {
            Hub = Substitute.For<IHub>();
            _ = SentrySdk.UseHub(Hub);
        }

        [Fact]
        public void CaptureEvent_MockInvoked()
        {
            var expected = new SentryEvent();
            _ = HubAdapter.Instance.CaptureEvent(expected);
            _ = Hub.Received(1).CaptureEvent(expected);
        }

        [Fact]
        public void CaptureEvent_WithScope_MockInvoked()
        {
            var expectedEvent = new SentryEvent();
            var expectedScope = new Scope();
            _ = HubAdapter.Instance.CaptureEvent(expectedEvent, expectedScope);
            _ = Hub.Received(1).CaptureEvent(expectedEvent, expectedScope);
        }

        [Fact]
        public void CaptureException_MockInvoked()
        {
            var expected = new Exception();
            _ = HubAdapter.Instance.CaptureException(expected);
            _ = Hub.Received(1).CaptureException(expected);
        }

        [Fact]
        public void IsEnabled_MockInvoked()
        {
            var isEnabled = HubAdapter.Instance.IsEnabled;
            Assert.False(isEnabled);
            isEnabled = Hub.Received(1).IsEnabled;
            Assert.False(isEnabled);
        }

        [Fact]
        public void LastEventId_MockInvoked()
        {
            _ = HubAdapter.Instance.LastEventId;
            _ = Hub.Received(1).LastEventId;
        }

        [Fact]
        public void ConfigureScopeAsync_MockInvoked()
        {
            static Task Expected(Scope _) => default;

            _ = HubAdapter.Instance.ConfigureScopeAsync(Expected);
            _ = Hub.Received(1).ConfigureScopeAsync(Expected);
        }

        [Fact]
        public void ConfigureScope_MockInvoked()
        {
            void Expected(Scope _)
            { }
            HubAdapter.Instance.ConfigureScope(Expected);
            Hub.Received(1).ConfigureScope(Expected);
        }

        [Fact]
        public void WithScope_MockInvoked()
        {
            void Expected(Scope _)
            { }
            HubAdapter.Instance.WithScope(Expected);
            Hub.Received(1).WithScope(Expected);
        }

        [Fact]
        public void PushScope_MockInvoked()
        {
            _ = HubAdapter.Instance.PushScope();
            _ = Hub.Received(1).PushScope();
        }

        [Fact]
        public void PushScope_State_MockInvoked()
        {
            var expected = new object();
            _ = HubAdapter.Instance.PushScope(expected);
            _ = Hub.Received(1).PushScope(expected);
        }

        [Fact]
        public void BindClient_MockInvoked()
        {
            var expected = Substitute.For<ISentryClient>();
            HubAdapter.Instance.BindClient(expected);
            Hub.Received(1).BindClient(expected);
        }

        [Fact]
        public void AddBreadcrumb_BreadcrumbInstanceCreated()
        {
            TestAddBreadcrumbExtension(HubAdapter.Instance.AddBreadcrumb);
        }

        [Fact]
        public void AddBreadcrumb_WithClock_BreadcrumbInstanceCreated()
        {
            var clock = Substitute.For<ISystemClock>();
            _ = clock.GetUtcNow().Returns(DateTimeOffset.MaxValue);

            TestAddBreadcrumbExtension((message, category, type, data, level)
                => HubAdapter.Instance.AddBreadcrumb(
                    clock,
                    message,
                    category,
                    type,
                    data,
                    level));

            _ = clock.Received(1).GetUtcNow();
        }

        private void TestAddBreadcrumbExtension(
            Action<
                string,
                string,
                string,
                IDictionary<string, string>,
                BreadcrumbLevel> action)
        {
            const string message = "message";
            const string type = "type";
            const string category = "category";
            var data = new Dictionary<string, string>
            {
                {"Key", "value"},
                {"Key2", "value2"},
            };
            const BreadcrumbLevel level = BreadcrumbLevel.Critical;

            var scope = new Scope();
            Hub.When(h => h.ConfigureScope(Arg.Any<Action<Scope>>()))
                .Do(c => c.Arg<Action<Scope>>()(scope));

            action(message, category, type, data, level);

            var crumb = scope.Breadcrumbs.First();
            Assert.Equal(message, crumb.Message);
            Assert.Equal(type, crumb.Type);
            Assert.Equal(category, crumb.Category);
            Assert.Equal(level, crumb.Level);
            Assert.Equal(data.Count, crumb.Data.Count);
            Assert.Equal(data.ToImmutableDictionary(), crumb.Data);
        }
    }
}
