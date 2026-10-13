#nullable enable

namespace Sentry.Tests;

public class HubExtensionsMonitorTests
{
    private const string MonitorSlug = "my-monitor";

    private readonly IHub _hub = Substitute.For<IHub>();
    private SentryId _checkInId = SentryId.Create();
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
    public void WithMonitor_Func_Throws_CapturesErrorAndRethrows()
    {
        var expected = new InvalidOperationException();
        Func<int> job = () => throw expected;

        var actual = Assert.Throws<InvalidOperationException>(() => _hub.WithMonitor(MonitorSlug, job));

        Assert.Same(expected, actual);
        AssertCheckIns(CheckInStatus.Error);
    }

    [Fact]
    public void WithMonitor_InProgressCheckInNotCaptured_FinalCheckInKeepsId()
    {
        _checkInId = SentryId.Empty;

        _hub.WithMonitor(MonitorSlug, () => { });

        AssertCheckIns(CheckInStatus.Ok);
    }

    [Fact]
    public async Task WithMonitor_Task_Succeeds_CapturesInProgressThenOk()
    {
        var ran = false;

        await _hub.WithMonitorAsync(MonitorSlug, async () =>
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
        var result = await _hub.WithMonitorAsync(MonitorSlug, async () =>
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
            _hub.WithMonitorAsync(MonitorSlug, async () =>
            {
                await Task.Yield();
                throw expected;
            }));

        Assert.Same(expected, actual);
        AssertCheckIns(CheckInStatus.Error);
    }

    [Fact]
    public async Task WithMonitor_TaskOfT_Throws_CapturesErrorAndRethrows()
    {
        var expected = new InvalidOperationException();

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _hub.WithMonitorAsync<int>(MonitorSlug, async () =>
            {
                await Task.Yield();
                throw expected;
            }));

        Assert.Same(expected, actual);
        AssertCheckIns(CheckInStatus.Error);
    }

    [Fact]
    public async Task WithMonitor_Task_InProgressCheckInNotCaptured_FinalCheckInKeepsId()
    {
        _checkInId = SentryId.Empty;

        await _hub.WithMonitorAsync(MonitorSlug, () => Task.CompletedTask);

        AssertCheckIns(CheckInStatus.Ok);
    }

    [Fact]
    public async Task WithMonitor_Task_NotCompleted_OnlyCapturesInProgress()
    {
        var tcs = new TaskCompletionSource<bool>();

        var task = _hub.WithMonitorAsync(MonitorSlug, () => (Task)tcs.Task);

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

        await _hub.WithMonitorAsync(MonitorSlug, () => Task.CompletedTask, configure);

        Assert.Equal(2, _checkIns.Count);
        Assert.Same(configure, _checkIns[0].ConfigureMonitorOptions);
        Assert.Null(_checkIns[1].ConfigureMonitorOptions);
    }

    [Fact]
    public async Task WithMonitor_Task_SetsDurationOnCompletion()
    {
        var delay = TimeSpan.FromMilliseconds(50);

        await _hub.WithMonitorAsync(MonitorSlug, () => Task.Delay(delay));

        var duration = _checkIns[1].Duration;
        Assert.NotNull(duration);
        Assert.True(duration >= delay - TimeSpan.FromMilliseconds(10), $"Duration was {duration}");
    }

    [Fact]
    public void WithMonitor_JobReturnsTask_ThrowsWithoutCheckIn()
    {
        var ran = false;
        ValueTask ValueTaskJob()
        { ran = true; return default; }
        ValueTask<int> ValueTaskOfTJob()
        { ran = true; return new ValueTask<int>(1); }
        Task TaskJob()
        { ran = true; return Task.CompletedTask; }

        Assert.Throws<ArgumentException>("job", () => { _ = _hub.WithMonitor(MonitorSlug, () => ValueTaskJob()); });
        Assert.Throws<ArgumentException>("job", () => { _ = _hub.WithMonitor(MonitorSlug, ValueTaskJob); });
        Assert.Throws<ArgumentException>("job", () => { _ = _hub.WithMonitor(MonitorSlug, () => ValueTaskOfTJob()); });
        Assert.Throws<ArgumentException>("job", () => { _ = _hub.WithMonitor<Task>(MonitorSlug, () => TaskJob()); });
        Assert.Throws<ArgumentException>("job", () => { _ = _hub.WithMonitor(MonitorSlug, () => Task.FromResult(1)); });

        Assert.False(ran);
        Assert.Empty(_checkIns);
    }

    // Pins which overload each job shape binds to. Jobs that bind to an async-aware path send ok only after the job
    // completes; the static return type is checked so a change in binding fails to compile or fails here.
    [Fact]
    public async Task WithMonitor_Binding_AsyncLambda_UsesTaskOverload()
    {
        var task = _hub.WithMonitorAsync(MonitorSlug, async () => { await PendingTask(); });

        Assert.Equal(typeof(Task), StaticType(task));
        await AssertOkSentAfterCompletion(task);
    }

    [Fact]
    public async Task WithMonitor_Binding_AsyncLambdaWithResult_UsesTaskOfTOverload()
    {
        var task = _hub.WithMonitorAsync(MonitorSlug, async () => await PendingValueTaskOfInt());

        Assert.Equal(typeof(Task<int>), StaticType(task));
        await AssertOkSentAfterCompletion(task);
    }

    [Fact]
    public async Task WithMonitor_Binding_AsyncLambdaAwaitingValueTask_UsesTaskOverload()
    {
        var task = _hub.WithMonitorAsync(MonitorSlug, async () => await PendingValueTask());

        Assert.Equal(typeof(Task), StaticType(task));
        await AssertOkSentAfterCompletion(task);
    }

    [Fact]
    public async Task WithMonitor_Binding_TaskExpressionLambda_UsesTaskOverload()
    {
        var task = _hub.WithMonitorAsync(MonitorSlug, () => PendingTask());

        Assert.Equal(typeof(Task), StaticType(task));
        await AssertOkSentAfterCompletion(task);
    }

    [Fact]
    public async Task WithMonitor_Binding_TaskMethodGroup_UsesTaskOverload()
    {
        var task = _hub.WithMonitorAsync(MonitorSlug, PendingTask);

        Assert.Equal(typeof(Task), StaticType(task));
        await AssertOkSentAfterCompletion(task);
    }

    [Fact]
    public async Task WithMonitor_Binding_TaskOfTMethodGroup_UsesTaskOfTOverload()
    {
        var task = _hub.WithMonitorAsync(MonitorSlug, PendingTaskOfInt);

        Assert.Equal(typeof(Task<int>), StaticType(task));
        await AssertOkSentAfterCompletion(task);
    }

    [Fact]
    public void WithMonitor_Binding_AsyncLambda_UsesFuncOfTAndThrows()
    {
        // Binds to Func<T> with T = Task, so the guard catches it.
        Assert.Throws<ArgumentException>("job", () => { _ = _hub.WithMonitor(MonitorSlug, async () => await PendingTask()); });
        Assert.Throws<ArgumentException>("job", () => { _ = _hub.WithMonitor(MonitorSlug, async () => { await PendingTask(); }); });

        Assert.Empty(_checkIns);
    }

    [Fact]
    public void WithMonitor_Binding_LambdasThatOnlyThrow_UseAction()
    {
        // With a Func<Task> overload under the same name, these bound to it and the exception was lost in the task.
        Assert.Throws<InvalidOperationException>(() =>
            _hub.WithMonitor(MonitorSlug, () => throw new InvalidOperationException()));
        Assert.Throws<InvalidOperationException>(() =>
            _hub.WithMonitor(MonitorSlug, () => { DoNothing(); throw new InvalidOperationException(); }));

        Assert.Equal(4, _checkIns.Count);
        Assert.Equal(CheckInStatus.Error, _checkIns[1].Status);
        Assert.Equal(CheckInStatus.Error, _checkIns[3].Status);
    }

    [Fact]
    public void WithMonitor_Binding_ValueExpressionLambda_UsesFuncOfT()
    {
        var result = _hub.WithMonitor(MonitorSlug, () => Compute());

        Assert.Equal(typeof(int), StaticType(result));
        AssertCheckIns(CheckInStatus.Ok);
    }

    [Fact]
    public void WithMonitor_Binding_VoidLambdas_UseAction()
    {
        _hub.WithMonitor(MonitorSlug, () => DoNothing());
        _hub.WithMonitor(MonitorSlug, DoNothing);
        _hub.WithMonitor(MonitorSlug, () => { });

        Assert.Equal(6, _checkIns.Count);
    }

    [Fact]
    public void WithMonitor_NullJob_ThrowsWithoutCheckIn()
    {
        Assert.Throws<ArgumentNullException>("job", () => _hub.WithMonitor(MonitorSlug, (Action)null!));
        Assert.Throws<ArgumentNullException>("job", () => _hub.WithMonitor(MonitorSlug, (Func<int>)null!));
        Assert.Throws<ArgumentNullException>("job", () => { _ = _hub.WithMonitorAsync(MonitorSlug, (Func<Task>)null!); });
        Assert.Throws<ArgumentNullException>("job", () => { _ = _hub.WithMonitorAsync(MonitorSlug, (Func<Task<int>>)null!); });

        Assert.Empty(_checkIns);
    }

    [Fact]
    public void WithMonitor_NullOrEmptySlug_ThrowsWithoutCheckIn()
    {
        Assert.Throws<ArgumentNullException>("monitorSlug", () => _hub.WithMonitor(null!, () => { }));
        Assert.Throws<ArgumentException>("monitorSlug", () => _hub.WithMonitor(" ", () => 1));
        Assert.Throws<ArgumentException>("monitorSlug", () => { _ = _hub.WithMonitorAsync("", () => Task.CompletedTask); });

        Assert.Empty(_checkIns);
    }

    [Fact]
    public void WithMonitor_TwoRuns_EachRunHasItsOwnTrace()
    {
        var fixture = new TraceFixture();
        var parentTraceId = fixture.ScopeTraceId();

        fixture.Hub.WithMonitor(MonitorSlug, () => { });
        fixture.Hub.WithMonitor(MonitorSlug, () => { });

        Assert.Equal(4, fixture.CheckInTraceIds.Count);
        Assert.Equal(fixture.CheckInTraceIds[0], fixture.CheckInTraceIds[1]);
        Assert.Equal(fixture.CheckInTraceIds[2], fixture.CheckInTraceIds[3]);
        Assert.NotEqual(fixture.CheckInTraceIds[0], fixture.CheckInTraceIds[2]);
        Assert.DoesNotContain(parentTraceId, fixture.CheckInTraceIds);
        Assert.Equal(parentTraceId, fixture.ScopeTraceId());
    }

    [Fact]
    public void WithMonitor_ExceptionCapturedInJob_SharesTheRunsTrace()
    {
        var fixture = new TraceFixture();

        fixture.Hub.WithMonitor(MonitorSlug, () => { fixture.Hub.CaptureException(new InvalidOperationException()); });

        var eventTraceId = Assert.Single(fixture.EventTraceIds);
        Assert.All(fixture.CheckInTraceIds, id => Assert.Equal(eventTraceId, id));
    }

    [Fact]
    public async Task WithMonitor_Task_ExceptionCapturedAfterAwait_SharesTheRunsTrace()
    {
        var fixture = new TraceFixture();
        var parentTraceId = fixture.ScopeTraceId();

        await fixture.Hub.WithMonitorAsync(MonitorSlug, async () =>
        {
            await Task.Yield();
            fixture.Hub.CaptureException(new InvalidOperationException());
        });

        var eventTraceId = Assert.Single(fixture.EventTraceIds);
        Assert.NotEqual(parentTraceId, eventTraceId);
        Assert.Equal(2, fixture.CheckInTraceIds.Count);
        Assert.All(fixture.CheckInTraceIds, id => Assert.Equal(eventTraceId, id));
        Assert.Equal(parentTraceId, fixture.ScopeTraceId());
    }

    [Fact]
    public async Task WithMonitor_ActiveTransaction_UsesItsTrace()
    {
        var fixture = new TraceFixture();
        var transaction = fixture.Hub.StartTransaction("name", "op");
        fixture.Hub.ConfigureScope(scope => scope.Transaction = transaction);

        fixture.Hub.WithMonitor(MonitorSlug, () => { });
        await fixture.Hub.WithMonitorAsync(MonitorSlug, () => Task.CompletedTask);

        Assert.Equal(4, fixture.CheckInTraceIds.Count);
        Assert.All(fixture.CheckInTraceIds, id => Assert.Equal(transaction.TraceId, id));
    }

    [Fact]
    public void WithMonitor_GlobalMode_KeepsTheScopesTrace()
    {
        var fixture = new TraceFixture(globalMode: true);
        var parentTraceId = fixture.ScopeTraceId();

        fixture.Hub.WithMonitor(MonitorSlug, () => { });

        Assert.All(fixture.CheckInTraceIds, id => Assert.Equal(parentTraceId, id));
        Assert.Equal(parentTraceId, fixture.ScopeTraceId());
    }

    private sealed class TraceFixture
    {
        private readonly ISentryClient _client = Substitute.For<ISentryClient>();

        public Hub Hub { get; }
        public List<SentryId> CheckInTraceIds { get; } = new();
        public List<SentryId> EventTraceIds { get; } = new();

        public TraceFixture(bool globalMode = false)
        {
            // Mirrors SentryClient.CaptureCheckIn, which reads the trace from the scope when the check-in is captured
            _client.CaptureCheckIn(Arg.Any<string>(), Arg.Any<CheckInStatus>(), Arg.Any<SentryId?>(),
                    Arg.Any<TimeSpan?>(), Arg.Any<Scope>(), Arg.Any<Action<SentryMonitorOptions>>())
                .Returns(ci =>
                {
                    var scope = ci.ArgAt<Scope>(4);
                    CheckInTraceIds.Add(scope.Span?.TraceId ?? scope.PropagationContext.TraceId);
                    return SentryId.Create();
                });
            _client.CaptureEvent(Arg.Any<SentryEvent>(), Arg.Any<Scope>(), Arg.Any<SentryHint>())
                .Returns(ci =>
                {
                    EventTraceIds.Add(ci.ArgAt<SentryEvent>(0).Contexts.Trace.TraceId);
                    return SentryId.Create();
                });

            var options = new SentryOptions
            {
                Dsn = ValidDsn,
                AutoSessionTracking = false,
                TracesSampleRate = 1.0,
                IsGlobalModeEnabled = globalMode
            };
            Hub = new Hub(options, _client);
        }

        public SentryId ScopeTraceId() => Hub.ScopeManager.GetCurrent().Key.PropagationContext.TraceId;
    }

    private readonly TaskCompletionSource<bool> _pending = new();

    private Task PendingTask() => _pending.Task;

    private async Task<int> PendingTaskOfInt()
    {
        await _pending.Task;
        return 1;
    }

    private async ValueTask PendingValueTask() => await _pending.Task;

    private async ValueTask<int> PendingValueTaskOfInt()
    {
        await _pending.Task;
        return 1;
    }

    private static int Compute() => 1;

    private static void DoNothing()
    {
    }

    private static Type StaticType<TValue>(TValue _) => typeof(TValue);

    private async Task AssertOkSentAfterCompletion(Task job)
    {
        var inProgress = Assert.Single(_checkIns);
        Assert.Equal(CheckInStatus.InProgress, inProgress.Status);

        _pending.SetResult(true);
        await job;

        AssertCheckIns(CheckInStatus.Ok);
    }

    private void AssertCheckIns(CheckInStatus finalStatus)
    {
        Assert.Collection(_checkIns,
            first =>
            {
                Assert.Equal(CheckInStatus.InProgress, first.Status);
                Assert.NotNull(first.SentryId);
                Assert.NotEqual(SentryId.Empty, first.SentryId);
                Assert.Null(first.Duration);
            },
            second =>
            {
                Assert.Equal(finalStatus, second.Status);
                Assert.Equal(_checkIns[0].SentryId, second.SentryId);
                Assert.NotNull(second.Duration);
                Assert.True(second.Duration >= TimeSpan.Zero);
            });
    }
}
