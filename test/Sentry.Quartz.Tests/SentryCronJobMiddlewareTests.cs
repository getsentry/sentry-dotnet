using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;

namespace Sentry.Quartz.Tests;

public class SentryCronJobMiddlewareTests
{
    private readonly IHub _hub = Substitute.For<IHub>();
    private readonly RecordingLogger<SentryCronJobMiddleware> _logger = new();
    private readonly SentryCronJobOptions _options = new();

    private SentryCronJobMiddleware CreateSut() =>
        new(_hub, Options.Create(_options), _logger);

    [Fact]
    public async Task Invoke_TriggerIsNotCronTrigger_DoesNotCaptureCheckIn()
    {
        var sut = CreateSut();
        var context = MiddlewareTestHelpers.CreateContext(new MonitoredJob(), Substitute.For<ITrigger>());

        await sut.Invoke(context, MiddlewareTestHelpers.Next(), CancellationToken.None);

        _hub.DidNotReceiveWithAnyArgs().CaptureCheckIn(default!, default);
    }

    [Fact]
    public async Task Invoke_CronTrigger_JobWithoutAttribute_DoesNotCaptureCheckIn()
    {
        var sut = CreateSut();
        var context = MiddlewareTestHelpers.CreateContext(new PlainJob(), MiddlewareTestHelpers.CreateCronTrigger());

        await sut.Invoke(context, MiddlewareTestHelpers.Next(), CancellationToken.None);

        _hub.DidNotReceiveWithAnyArgs().CaptureCheckIn(default!, default);
    }

    [Fact]
    public async Task Invoke_CronTrigger_JobWithAttribute_JobSucceeds_CapturesInProgressThenOkWithSameId()
    {
        var startId = SentryId.Create();
        _hub.CaptureCheckIn(nameof(MonitoredJob), CheckInStatus.InProgress, configureMonitorOptions: Arg.Any<Action<SentryMonitorOptions>>())
            .Returns(startId);

        var sut = CreateSut();
        var context = MiddlewareTestHelpers.CreateContext(new MonitoredJob(), MiddlewareTestHelpers.CreateCronTrigger());

        await sut.Invoke(context, MiddlewareTestHelpers.Next(), CancellationToken.None);

        _hub.Received(1).CaptureCheckIn(nameof(MonitoredJob), CheckInStatus.InProgress, configureMonitorOptions: Arg.Any<Action<SentryMonitorOptions>>());
        _hub.Received(1).CaptureCheckIn(nameof(MonitoredJob), CheckInStatus.Ok, startId);
    }

    [Fact]
    public async Task Invoke_CronTrigger_JobWithAttribute_JobThrows_CapturesInProgressThenErrorAndRethrows()
    {
        var startId = SentryId.Create();
        _hub.CaptureCheckIn(nameof(MonitoredJob), CheckInStatus.InProgress, configureMonitorOptions: Arg.Any<Action<SentryMonitorOptions>>())
            .Returns(startId);

        var sut = CreateSut();
        var context = MiddlewareTestHelpers.CreateContext(new MonitoredJob(), MiddlewareTestHelpers.CreateCronTrigger());
        var exception = new InvalidOperationException("job failed");

        var act = () => sut.Invoke(context, MiddlewareTestHelpers.Next(exception), CancellationToken.None).AsTask();

        (await act.Should().ThrowAsync<InvalidOperationException>()).Which.Should().BeSameAs(exception);
        _hub.Received(1).CaptureCheckIn(nameof(MonitoredJob), CheckInStatus.InProgress, configureMonitorOptions: Arg.Any<Action<SentryMonitorOptions>>());
        _hub.Received(1).CaptureCheckIn(nameof(MonitoredJob), CheckInStatus.Error, startId);
    }

    [Fact]
    public async Task Invoke_CronTrigger_CustomMonitorSlug_UsesSlugFromAttribute()
    {
        _hub.CaptureCheckIn(Arg.Any<string>(), Arg.Any<CheckInStatus>(), configureMonitorOptions: Arg.Any<Action<SentryMonitorOptions>>())
            .Returns(SentryId.Create());

        var sut = CreateSut();
        var context = MiddlewareTestHelpers.CreateContext(new CustomSlugJob(), MiddlewareTestHelpers.CreateCronTrigger());

        await sut.Invoke(context, MiddlewareTestHelpers.Next(), CancellationToken.None);

        _hub.Received(1).CaptureCheckIn("custom-slug", CheckInStatus.InProgress, configureMonitorOptions: Arg.Any<Action<SentryMonitorOptions>>());
    }

    [Fact]
    public async Task Invoke_EnableUpsertCronMonitorFalse_DoesNotConfigureScheduleOrTimeZone_AndSkipsUserCallback()
    {
        _options.EnableUpsertCronMonitor = false;
        var callbackInvoked = false;
        _options.ConfigureSentryMonitorOptions = (_, _) => callbackInvoked = true;

        var configureCallback = CaptureConfigureCallback();

        var sut = CreateSut();
        var context = MiddlewareTestHelpers.CreateContext(new MonitoredJob(), MiddlewareTestHelpers.CreateCronTrigger());

        await sut.Invoke(context, MiddlewareTestHelpers.Next(), CancellationToken.None);

        var monitorOptions = new SentryMonitorOptions();
        configureCallback().Invoke(monitorOptions);

        callbackInvoked.Should().BeFalse();
        monitorOptions.TimeZone.Should().BeNull();
        // Interval was never set, so setting it now should succeed rather than throw "set twice".
        monitorOptions.Invoking(o => o.Interval("* * * * *")).Should().NotThrow();
    }

    [Fact]
    public async Task Invoke_EnableUpsertCronMonitorTrue_SetsScheduleAndUtcTimeZone()
    {
        var configureCallback = CaptureConfigureCallback();

        var sut = CreateSut();
        var context = MiddlewareTestHelpers.CreateContext(
            new MonitoredJob(),
            MiddlewareTestHelpers.CreateCronTrigger("0 30 9 * * ?", TimeZoneInfo.Utc));

        await sut.Invoke(context, MiddlewareTestHelpers.Next(), CancellationToken.None);

        var monitorOptions = new SentryMonitorOptions();
        configureCallback().Invoke(monitorOptions);

        monitorOptions.TimeZone.Should().Be("Etc/UTC");
        using var json = monitorOptions.ToJsonDocument();
        var schedule = json.RootElement.GetProperty("monitor_config").GetProperty("schedule");
        schedule.GetProperty("type").GetString().Should().Be("crontab");
        schedule.GetProperty("value").GetString().Should().Be("30 9 * * *");
    }

    [Fact]
    public async Task Invoke_TimeZoneIsAlreadyIana_UsesItAsIs()
    {
        var configureCallback = CaptureConfigureCallback();
        var ianaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

        var sut = CreateSut();
        var context = MiddlewareTestHelpers.CreateContext(
            new MonitoredJob(),
            MiddlewareTestHelpers.CreateCronTrigger(timeZone: ianaTimeZone));

        await sut.Invoke(context, MiddlewareTestHelpers.Next(), CancellationToken.None);

        var monitorOptions = new SentryMonitorOptions();
        configureCallback().Invoke(monitorOptions);

        monitorOptions.TimeZone.Should().Be("America/New_York");
    }

    [Fact]
    public async Task Invoke_UserConfigureCallback_RunsAfterAutomaticConfiguration_AndCanOverrideIt()
    {
        _options.ConfigureSentryMonitorOptions = (_, options) =>
        {
            options.TimeZone = "custom/zone";
            options.FailureIssueThreshold = 7;
        };
        var configureCallback = CaptureConfigureCallback();

        var sut = CreateSut();
        var context = MiddlewareTestHelpers.CreateContext(new MonitoredJob(), MiddlewareTestHelpers.CreateCronTrigger());

        await sut.Invoke(context, MiddlewareTestHelpers.Next(), CancellationToken.None);

        var monitorOptions = new SentryMonitorOptions();
        configureCallback().Invoke(monitorOptions);

        monitorOptions.TimeZone.Should().Be("custom/zone");
        monitorOptions.FailureIssueThreshold.Should().Be(7);
    }

    [Fact]
    public async Task Invoke_UserConfigureCallback_ReceivesTheExecutingJobDetail()
    {
        IJobDetail? receivedJobDetail = null;
        _options.ConfigureSentryMonitorOptions = (jobDetail, _) => receivedJobDetail = jobDetail;
        var configureCallback = CaptureConfigureCallback();

        var jobDetail = Substitute.For<IJobDetail>();
        var sut = CreateSut();
        var context = MiddlewareTestHelpers.CreateContext(new MonitoredJob(), MiddlewareTestHelpers.CreateCronTrigger(), jobDetail);

        await sut.Invoke(context, MiddlewareTestHelpers.Next(), CancellationToken.None);
        configureCallback().Invoke(new SentryMonitorOptions());

        receivedJobDetail.Should().BeSameAs(jobDetail);
    }

    [Fact]
    public async Task Invoke_CronExpressionWithSecondsField_TrimsSecondsAndWarnsOnce()
    {
        var configureCallback = CaptureConfigureCallback();

        var sut = CreateSut();

        // "30" as the seconds field is not supported by Sentry Cron Monitors and should be trimmed off,
        // triggering a granularity warning.
        var firstContext = MiddlewareTestHelpers.CreateContext(new MonitoredJob(), MiddlewareTestHelpers.CreateCronTrigger("30 0 12 * * ?"));
        await sut.Invoke(firstContext, MiddlewareTestHelpers.Next(), CancellationToken.None);

        var monitorOptions = new SentryMonitorOptions();
        configureCallback().Invoke(monitorOptions);
        using var json = monitorOptions.ToJsonDocument();
        json.RootElement.GetProperty("monitor_config").GetProperty("schedule").GetProperty("value").GetString()
            .Should().Be("0 12 * * *");

        // Invoking the middleware again for the same job type should not warn a second time.
        var secondContext = MiddlewareTestHelpers.CreateContext(new MonitoredJob(), MiddlewareTestHelpers.CreateCronTrigger("30 0 12 * * ?"));
        await sut.Invoke(secondContext, MiddlewareTestHelpers.Next(), CancellationToken.None);

        _logger.Entries.Should().ContainSingle(e => e.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task Invoke_CronExpressionWithZeroSecondsField_DoesNotWarn()
    {
        var sut = CreateSut();
        var context = MiddlewareTestHelpers.CreateContext(new MonitoredJob(), MiddlewareTestHelpers.CreateCronTrigger("0 0 12 * * ?"));

        await sut.Invoke(context, MiddlewareTestHelpers.Next(), CancellationToken.None);

        _logger.Entries.Should().NotContain(e => e.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task Invoke_CronExpressionValidForQuartzButNotForSentry_LogsErrorAndDoesNotThrow()
    {
        var configureCallback = CaptureConfigureCallback();

        var sut = CreateSut();
        // "5L" (last Friday of the month) is valid Quartz syntax, but isn't supported by Sentry's crontab format.
        var context = MiddlewareTestHelpers.CreateContext(new MonitoredJob(), MiddlewareTestHelpers.CreateCronTrigger("0 0 * * 5L"));

        await sut.Invoke(context, MiddlewareTestHelpers.Next(), CancellationToken.None);

        var monitorOptions = new SentryMonitorOptions();
        var act = () => configureCallback().Invoke(monitorOptions);

        act.Should().NotThrow();
        _logger.Entries.Should().ContainSingle(e => e.Level == LogLevel.Error);
        // The schedule was never successfully applied, so setting it now should succeed rather than throw "set twice".
        monitorOptions.Invoking(o => o.Interval("* * * * *")).Should().NotThrow();
    }

    /// <summary>
    /// Captures the <c>configureMonitorOptions</c> callback that <see cref="SentryCronJobMiddleware"/> passes to
    /// <see cref="IHub.CaptureCheckIn"/>, so its effect on a real <see cref="SentryMonitorOptions"/> instance can be
    /// asserted on directly.
    /// </summary>
    private Func<Action<SentryMonitorOptions>> CaptureConfigureCallback()
    {
        Action<SentryMonitorOptions>? captured = null;
        _hub.CaptureCheckIn(Arg.Any<string>(), Arg.Any<CheckInStatus>(), configureMonitorOptions: Arg.Do<Action<SentryMonitorOptions>>(callback => captured = callback))
            .Returns(SentryId.Create());
        return () => captured ?? throw new InvalidOperationException("configureMonitorOptions was not captured; CaptureCheckIn was not called.");
    }
}
