#nullable enable

using System.Threading.Tasks.Sources;

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
    public void WithMonitor_InProgressCheckInNotCaptured_FinalCheckInGetsNewId()
    {
        _checkInId = SentryId.Empty;

        _hub.WithMonitor(MonitorSlug, () => { });

        Assert.Equal(2, _checkIns.Count);
        Assert.Equal(CheckInStatus.Ok, _checkIns[1].Status);
        Assert.Null(_checkIns[1].SentryId);
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
    public async Task WithMonitor_TaskOfT_Throws_CapturesErrorAndRethrows()
    {
        var expected = new InvalidOperationException();

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _hub.WithMonitor<int>(MonitorSlug, async () =>
            {
                await Task.Yield();
                throw expected;
            }));

        Assert.Same(expected, actual);
        AssertCheckIns(CheckInStatus.Error);
    }

    [Fact]
    public async Task WithMonitor_Task_InProgressCheckInNotCaptured_FinalCheckInGetsNewId()
    {
        _checkInId = SentryId.Empty;

        await _hub.WithMonitor(MonitorSlug, () => Task.CompletedTask);

        Assert.Equal(2, _checkIns.Count);
        Assert.Equal(CheckInStatus.Ok, _checkIns[1].Status);
        Assert.Null(_checkIns[1].SentryId);
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

    [Fact]
    public async Task WithMonitor_ValueTask_Succeeds_CapturesOkAfterCompletion()
    {
        var delay = TimeSpan.FromMilliseconds(50);

        var valueTask = _hub.WithMonitor(MonitorSlug, () => PendingValueTask());

        Assert.Single(_checkIns);
        await Task.Delay(delay);
        _pending.SetResult(true);
        await valueTask;

        AssertCheckIns(CheckInStatus.Ok);
        Assert.True(_checkIns[1].Duration >= delay - TimeSpan.FromMilliseconds(10), $"Duration was {_checkIns[1].Duration}");
    }

    [Fact]
    public void WithMonitor_ValueTask_ThrowsBeforeReturning_CapturesErrorAndRethrows()
    {
        var expected = new InvalidOperationException();
        Func<ValueTask> job = () => throw expected;

        var actual = Assert.Throws<InvalidOperationException>(() => _hub.WithMonitor(MonitorSlug, job));

        Assert.Same(expected, actual);
        AssertCheckIns(CheckInStatus.Error);
    }

    [Fact]
    public async Task WithMonitor_ValueTask_ThrowsBeforeAwait_CapturesErrorAndRethrowsOnAwait()
    {
        var expected = new InvalidOperationException();
#pragma warning disable CS1998 // Async method lacks 'await' operators
        async ValueTask Job() => throw expected;
#pragma warning restore CS1998

        var valueTask = _hub.WithMonitor(MonitorSlug, Job);
        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() => valueTask.AsTask());

        Assert.Same(expected, actual);
        AssertCheckIns(CheckInStatus.Error);
    }

    [Fact]
    public async Task WithMonitor_ValueTask_ThrowsAfterAwait_CapturesErrorAndRethrowsOnAwait()
    {
        var expected = new InvalidOperationException();
        async ValueTask Job()
        {
            await Task.Yield();
            throw expected;
        }

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _hub.WithMonitor(MonitorSlug, () => Job()).AsTask());

        Assert.Same(expected, actual);
        AssertCheckIns(CheckInStatus.Error);
    }

    [Fact]
    public async Task WithMonitor_ValueTask_AwaitsSourceExactlyOnce()
    {
        var source = new CountingValueTaskSource();

        await _hub.WithMonitor(MonitorSlug, () => new ValueTask(source, 0));

        Assert.Equal(1, source.GetResultCalls);
        AssertCheckIns(CheckInStatus.Ok);
    }

    [Fact]
    public async Task WithMonitor_ValueTaskOfT_LogsWarningAndCapturesOkImmediately()
    {
        var logger = new InMemoryDiagnosticLogger();
        var previous = SentryClientExtensions.SentryOptionsForTestingOnly;
        SentryClientExtensions.SentryOptionsForTestingOnly = new SentryOptions { Debug = true, DiagnosticLogger = logger };
        try
        {
            var valueTask = _hub.WithMonitor(MonitorSlug, () => PendingValueTaskOfInt());

            // Existing behaviour: the job is not awaited, so ok is sent before it completes.
            AssertCheckIns(CheckInStatus.Ok);
            Assert.Contains(logger.Entries, e =>
                e.Level == SentryLevel.Warning && e.Message.Contains("async () => await job()") &&
                e.Args.Contains(MonitorSlug));

            _pending.SetResult(true);
            Assert.Equal(1, await valueTask);
        }
        finally
        {
            SentryClientExtensions.SentryOptionsForTestingOnly = previous;
        }
    }

    [Fact]
    public void WithMonitor_TaskWithExplicitFuncOfT_LogsWarningAndCapturesOkImmediately()
    {
        var logger = new InMemoryDiagnosticLogger();
        var previous = SentryClientExtensions.SentryOptionsForTestingOnly;
        SentryClientExtensions.SentryOptionsForTestingOnly = new SentryOptions { Debug = true, DiagnosticLogger = logger };
        try
        {
            _hub.WithMonitor<Task>(MonitorSlug, () => PendingTask());

            AssertCheckIns(CheckInStatus.Ok);
            Assert.Contains(logger.Entries, e => e.Level == SentryLevel.Warning && e.Args.Contains(MonitorSlug));
        }
        finally
        {
            SentryClientExtensions.SentryOptionsForTestingOnly = previous;
        }
    }

    // Pins which overload each job shape binds to. Jobs that bind to an async-aware path send ok only after the job
    // completes; the static return type is checked so a change in binding fails to compile or fails here.
    [Fact]
    public async Task WithMonitor_Binding_AsyncLambda_UsesTaskOverload()
    {
        var task = _hub.WithMonitor(MonitorSlug, async () => { await PendingTask(); });

        Assert.Equal(typeof(Task), StaticType(task));
        await AssertOkSentAfterCompletion(task);
    }

    [Fact]
    public async Task WithMonitor_Binding_AsyncLambdaWithResult_UsesTaskOfTOverload()
    {
        var task = _hub.WithMonitor(MonitorSlug, async () => await PendingValueTaskOfInt());

        Assert.Equal(typeof(Task<int>), StaticType(task));
        await AssertOkSentAfterCompletion(task);
    }

    [Fact]
    public async Task WithMonitor_Binding_AsyncLambdaAwaitingValueTask_UsesTaskOverload()
    {
        var task = _hub.WithMonitor(MonitorSlug, async () => await PendingValueTask());

        Assert.Equal(typeof(Task), StaticType(task));
        await AssertOkSentAfterCompletion(task);
    }

    [Fact]
    public async Task WithMonitor_Binding_TaskExpressionLambda_UsesTaskOverload()
    {
        var task = _hub.WithMonitor(MonitorSlug, () => PendingTask());

        Assert.Equal(typeof(Task), StaticType(task));
        await AssertOkSentAfterCompletion(task);
    }

    [Fact]
    public async Task WithMonitor_Binding_TaskMethodGroup_UsesTaskOverload()
    {
        var task = _hub.WithMonitor(MonitorSlug, PendingTask);

        Assert.Equal(typeof(Task), StaticType(task));
        await AssertOkSentAfterCompletion(task);
    }

    [Fact]
    public async Task WithMonitor_Binding_TaskOfTMethodGroup_UsesTaskOfTOverload()
    {
        var task = _hub.WithMonitor(MonitorSlug, PendingTaskOfInt);

        Assert.Equal(typeof(Task<int>), StaticType(task));
        await AssertOkSentAfterCompletion(task);
    }

    [Fact]
    public async Task WithMonitor_Binding_ValueTaskExpressionLambda_UsesFuncOfTAndAwaits()
    {
        // Binds to Func<T> (T = ValueTask), not Action: a value-returning lambda prefers the delegate with a return type.
        var valueTask = _hub.WithMonitor(MonitorSlug, () => PendingValueTask());

        Assert.Equal(typeof(ValueTask), StaticType(valueTask));
        await AssertOkSentAfterCompletion(valueTask.AsTask());
    }

    [Fact]
    public async Task WithMonitor_Binding_NewValueTaskLambda_UsesFuncOfTAndAwaits()
    {
        var valueTask = _hub.WithMonitor(MonitorSlug, () => new ValueTask(_pending.Task));

        Assert.Equal(typeof(ValueTask), StaticType(valueTask));
        await AssertOkSentAfterCompletion(valueTask.AsTask());
    }

    [Fact]
    public async Task WithMonitor_Binding_ValueTaskMethodGroup_UsesFuncOfTAndAwaits()
    {
        var valueTask = _hub.WithMonitor(MonitorSlug, PendingValueTask);

        Assert.Equal(typeof(ValueTask), StaticType(valueTask));
        await AssertOkSentAfterCompletion(valueTask.AsTask());
    }

    [Fact]
    public void WithMonitor_Binding_ValueTaskOfTExpressionLambda_UsesFuncOfT()
    {
        var valueTask = _hub.WithMonitor(MonitorSlug, () => PendingValueTaskOfInt());

        Assert.Equal(typeof(ValueTask<int>), StaticType(valueTask));
        AssertCheckIns(CheckInStatus.Ok);
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

    private sealed class CountingValueTaskSource : IValueTaskSource
    {
        public int GetResultCalls { get; private set; }

        public ValueTaskSourceStatus GetStatus(short token) => ValueTaskSourceStatus.Succeeded;

        public void OnCompleted(Action<object?> continuation, object? state, short token,
            ValueTaskSourceOnCompletedFlags flags) => continuation(state);

        public void GetResult(short token) => GetResultCalls++;
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
