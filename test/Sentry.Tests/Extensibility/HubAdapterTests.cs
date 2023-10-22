namespace Sentry.Tests.Extensibility;

[Collection(nameof(SentrySdkCollection))]
public class HubAdapterTests : IDisposable
{
    private IHubEx Hub { get; }

    public HubAdapterTests()
    {
        Hub = Substitute.For<IHubEx>();
        SentrySdk.UseHub(Hub);
    }

    [Fact]
    public void CaptureEvent_MockInvoked()
    {
        var expected = new SentryEvent();
        HubAdapter.Instance.CaptureEvent(expected);
        Hub.Received(1).CaptureEvent(expected);
    }

    [Fact]
    public void CaptureEvent_WithScope_MockInvoked()
    {
        var expectedEvent = new SentryEvent();
        var expectedScope = new Scope();
        HubAdapter.Instance.CaptureEvent(expectedEvent, expectedScope);
        Hub.Received(1).CaptureEvent(expectedEvent, expectedScope);
    }

    [Fact]
    public void CaptureException_MockInvoked()
    {
        var expected = new Exception();
        Hub.IsEnabled.Returns(true);
        HubAdapter.Instance.CaptureException(expected);
        Hub.Received(1).CaptureEventInternal(Arg.Is<SentryEvent>(s => s.Exception == expected));
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
    public void EndSession_CrashedStatus_MockInvoked()
    {
        var expected = SessionEndStatus.Crashed;
        HubAdapter.Instance.EndSession(expected);
        Hub.Received(1).EndSession(expected);
    }

    [Fact]
    public void ConfigureScopeAsync_MockInvoked()
    {
        static Task Expected(Scope _) => Task.CompletedTask;

        HubAdapter.Instance.ConfigureScopeAsync(Expected);
        Hub.Received(1).ConfigureScopeAsync(Expected);
    }

    [Fact]
    public void ConfigureScope_MockInvoked()
    {
        void Expected(Scope _) { }
        HubAdapter.Instance.ConfigureScope(Expected);
        Hub.Received(1).ConfigureScope(Expected);
    }

    [Fact]
    public void PushScope_MockInvoked()
    {
        HubAdapter.Instance.PushScope();
        Hub.Received(1).PushScope();
    }

    [Fact]
    public void PushScope_State_MockInvoked()
    {
        var expected = new object();
        HubAdapter.Instance.PushScope(expected);
        Hub.Received(1).PushScope(expected);
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
        clock.GetUtcNow().Returns(DateTimeOffset.MaxValue);

        TestAddBreadcrumbExtension((message, category, type, data, level)
            => HubAdapter.Instance.AddBreadcrumb(
                clock,
                message,
                category,
                type,
                data,
                level));

        clock.Received(1).GetUtcNow();
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
        Assert.Equal(data.Count, crumb.Data?.Count);
        Assert.Equal(data.ToImmutableDictionary(), crumb.Data);
    }

    public void Dispose()
    {
        SentrySdk.Close();
    }
}
