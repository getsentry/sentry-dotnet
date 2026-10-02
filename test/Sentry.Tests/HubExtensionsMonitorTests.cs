#nullable enable

namespace Sentry.Tests;

public class HubExtensionsMonitorTests
{
    private const string MonitorSlug = "my-monitor";

    private readonly IHub _hub = Substitute.For<IHub>();
    private readonly SentryId _checkInId = SentryId.Create();
    private readonly List<CheckInCall> _checkIns = new();

    private record CheckInCall(
        CheckInStatus Status,
        SentryId? SentryId,
        TimeSpan? Duration,
        Action<SentryMonitorOptions>? ConfigureMonitorOptions);

    public HubExtensionsMonitorTests()
    {
        _hub.CaptureCheckIn(Arg.Any<string>(), Arg.Any<CheckInStatus>(), Arg.Any<SentryId?>(), Arg.Any<TimeSpan?>(),
                Arg.Any<Scope>(), Arg.Any<Action<SentryMonitorOptions>>())
            .Returns(ci =>
            {
                Assert.Equal(MonitorSlug, ci.ArgAt<string>(0));
                _checkIns.Add(new CheckInCall(
                    ci.ArgAt<CheckInStatus>(1),
                    ci.ArgAt<SentryId?>(2),
                    ci.ArgAt<TimeSpan?>(3),
                    ci.ArgAt<Action<SentryMonitorOptions>?>(5)));
                return _checkInId;
            });
    }

    [Fact]
    public void WithMonitor_Action_Succeeds_CapturesInProgressThenOk()
    {
        var ran = false;

        _hub.WithMonitor(MonitorSlug, () => { ran = true; });

        Assert.True(ran);
        AssertCheckIns(CheckInStatus.Ok);
    }

    [Fact]
    public void WithMonitor_Func_Succeeds_ReturnsResult()
    {
        var result = _hub.WithMonitor(MonitorSlug, () => 42);

        Assert.Equal(42, result);
        AssertCheckIns(CheckInStatus.Ok);
    }

    [Fact]
    public void WithMonitor_Action_Throws_CapturesErrorAndRethrows()
    {
        var expected = new InvalidOperationException();
        Action job = () => throw expected;

        var actual = Assert.Throws<InvalidOperationException>(() => _hub.WithMonitor(MonitorSlug, job));

        Assert.Same(expected, actual);
        AssertCheckIns(CheckInStatus.Error);
    }

    [Fact]
    public async Task WithMonitor_Task_Succeeds_CapturesInProgressThenOk()
    {
        var ran = false;

        await _hub.WithMonitor(MonitorSlug, async () =>
        {
            await Task.Yield();
            ran = true;
        });

        Assert.True(ran);
        AssertCheckIns(CheckInStatus.Ok);
    }

    [Fact]
    public async Task WithMonitor_TaskOfT_Succeeds_ReturnsResult()
    {
        var result = await _hub.WithMonitor(MonitorSlug, async () =>
        {
            await Task.Yield();
            return "done";
        });

        Assert.Equal("done", result);
        AssertCheckIns(CheckInStatus.Ok);
    }

    [Fact]
    public async Task WithMonitor_Task_Throws_CapturesErrorAndRethrows()
    {
        var expected = new InvalidOperationException();

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _hub.WithMonitor(MonitorSlug, async () =>
            {
                await Task.Yield();
                throw expected;
            }));

        Assert.Same(expected, actual);
        AssertCheckIns(CheckInStatus.Error);
    }

    [Fact]
    public async Task WithMonitor_Task_NotCompleted_OnlyCapturesInProgress()
    {
        var tcs = new TaskCompletionSource<bool>();

        var task = _hub.WithMonitor(MonitorSlug, () => (Task)tcs.Task);

        Assert.Single(_checkIns);
        Assert.Equal(CheckInStatus.InProgress, _checkIns[0].Status);

        tcs.SetResult(true);
        await task;

        AssertCheckIns(CheckInStatus.Ok);
    }

    [Fact]
    public void WithMonitor_MonitorOptions_OnlySentWithInProgressCheckIn()
    {
        Action<SentryMonitorOptions> configure = options => options.Interval("* * * * *");

        _hub.WithMonitor(MonitorSlug, () => { }, configure);

        Assert.Equal(2, _checkIns.Count);
        Assert.Same(configure, _checkIns[0].ConfigureMonitorOptions);
        Assert.Null(_checkIns[1].ConfigureMonitorOptions);
    }

    [Fact]
    public async Task WithMonitor_Task_MonitorOptions_OnlySentWithInProgressCheckIn()
    {
        Action<SentryMonitorOptions> configure = options => options.Interval("* * * * *");

        await _hub.WithMonitor(MonitorSlug, () => Task.CompletedTask, configure);

        Assert.Equal(2, _checkIns.Count);
        Assert.Same(configure, _checkIns[0].ConfigureMonitorOptions);
        Assert.Null(_checkIns[1].ConfigureMonitorOptions);
    }

    [Fact]
    public async Task WithMonitor_Task_SetsDurationOnCompletion()
    {
        var delay = TimeSpan.FromMilliseconds(50);

        await _hub.WithMonitor(MonitorSlug, () => Task.Delay(delay));

        var duration = _checkIns[1].Duration;
        Assert.NotNull(duration);
        Assert.True(duration >= delay - TimeSpan.FromMilliseconds(10), $"Duration was {duration}");
    }

    private void AssertCheckIns(CheckInStatus finalStatus)
    {
        Assert.Collection(_checkIns,
            first =>
            {
                Assert.Equal(CheckInStatus.InProgress, first.Status);
                Assert.Null(first.SentryId);
                Assert.Null(first.Duration);
            },
            second =>
            {
                Assert.Equal(finalStatus, second.Status);
                Assert.Equal(_checkInId, second.SentryId);
                Assert.NotNull(second.Duration);
                Assert.True(second.Duration >= TimeSpan.Zero);
            });
    }
}
