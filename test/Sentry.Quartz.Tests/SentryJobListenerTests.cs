namespace Sentry.Quartz.Tests;

public class SentryJobListenerTests
{
    private class Fixture
    {
        private readonly Dictionary<object, object> _items = new();

        public IHub Hub { get; } = Substitute.For<IHub>();
        public IScheduler Scheduler { get; } = Substitute.For<IScheduler>();
        public SentryId CheckInId { get; } = SentryId.Create();
        public SentryQuartzOptions Options { get; } = new();
        public TimeSpan JobRunTime { get; } = TimeSpan.FromSeconds(3);
        public int TriggerCount { get; set; } = 1;

        public IJobDetail JobDetail { get; set; } = JobBuilder.Create<NoOpJob>()
            .WithIdentity("DailyEmail", "reports")
            .Build();

        public ITrigger Trigger { get; set; } = TriggerBuilder.Create()
            .WithCronSchedule("0 0 12 * * ?", schedule => schedule.InTimeZone(TimeZoneInfo.Utc))
            .Build();

        public SentryJobListener GetSut()
        {
            Hub.CaptureCheckIn(default!, default, default, default, default, default).ReturnsForAnyArgs(CheckInId);
            return new SentryJobListener(Options, Hub);
        }

        public IJobExecutionContext GetContext()
        {
            var mergedJobDataMap = new JobDataMap();
            mergedJobDataMap.PutAll(JobDetail.JobDataMap);
            mergedJobDataMap.PutAll(Trigger.JobDataMap);

            var triggers = Enumerable.Repeat(Trigger, TriggerCount).ToList();
            Scheduler.GetTriggersOfJob(JobDetail.Key, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyCollection<ITrigger>>(triggers));

            var context = Substitute.For<IJobExecutionContext>();
            context.JobDetail.Returns(JobDetail);
            context.Trigger.Returns(Trigger);
            context.Scheduler.Returns(Scheduler);
            context.MergedJobDataMap.Returns(mergedJobDataMap);
            context.JobRunTime.Returns(JobRunTime);
            context.When(c => c.Put(Arg.Any<object>(), Arg.Any<object>())).Do(call => _items[call[0]] = call[1]);
            context.Get(Arg.Any<object>()).Returns(call => _items.TryGetValue(call[0], out var value) ? value : null);
            return context;
        }
    }

    private readonly Fixture _fixture = new();

    [Fact]
    public async Task JobToBeExecuted_CronTrigger_CapturesInProgressCheckInWithMonitorConfig()
    {
        // Arrange
        var sut = _fixture.GetSut();

        // Act
        await sut.JobToBeExecuted(_fixture.GetContext());

        // Assert
        var monitorConfig = MonitorConfigJson.Render(_fixture.Hub.ReceivedConfigureMonitorOptions("reports-dailyemail")!);
        monitorConfig.GetProperty("schedule").GetProperty("value").GetString().Should().Be("0 12 * * *");
        monitorConfig.GetProperty("timezone").GetString().Should().Be("UTC");
    }

    [Fact]
    public async Task JobToBeExecuted_SendMonitorConfigDisabled_CapturesCheckInWithoutMonitorConfig()
    {
        // Arrange
        _fixture.Options.SendMonitorConfig = false;
        var sut = _fixture.GetSut();

        // Act
        await sut.JobToBeExecuted(_fixture.GetContext());

        // Assert
        _fixture.Hub.ReceivedConfigureMonitorOptions("reports-dailyemail").Should().BeNull();
        await _fixture.Scheduler.DidNotReceiveWithAnyArgs().GetTriggersOfJob(default!, default);
    }

    [Fact]
    public async Task JobToBeExecuted_ScheduleSentryCantRepresent_CapturesCheckInWithoutMonitorConfig()
    {
        // Arrange
        _fixture.Trigger = TriggerBuilder.Create()
            .WithCronSchedule("0 0 12 L * ?", schedule => schedule.InTimeZone(TimeZoneInfo.Utc))
            .Build();
        var sut = _fixture.GetSut();

        // Act
        await sut.JobToBeExecuted(_fixture.GetContext());

        // Assert
        _fixture.Hub.ReceivedConfigureMonitorOptions("reports-dailyemail").Should().BeNull();
    }

    [Fact]
    public async Task JobToBeExecuted_JobHasSeveralTriggers_CapturesCheckInWithoutMonitorConfig()
    {
        // Arrange
        _fixture.TriggerCount = 2;
        var sut = _fixture.GetSut();

        // Act
        await sut.JobToBeExecuted(_fixture.GetContext());

        // Assert
        _fixture.Hub.ReceivedConfigureMonitorOptions("reports-dailyemail").Should().BeNull();
    }

    [Fact]
    public async Task JobToBeExecuted_TriggerSetsMonitorSlug_CapturesCheckInWithThatTriggersMonitorConfig()
    {
        // Arrange
        _fixture.TriggerCount = 2;
        _fixture.Trigger = TriggerBuilder.Create()
            .WithCronSchedule("0 0 12 * * ?", schedule => schedule.InTimeZone(TimeZoneInfo.Utc))
            .UsingJobData(SentryJobListener.MonitorSlugKey, "daily-email-noon")
            .Build();
        var sut = _fixture.GetSut();

        // Act
        await sut.JobToBeExecuted(_fixture.GetContext());

        // Assert
        _fixture.Hub.ReceivedConfigureMonitorOptions("daily-email-noon").Should().NotBeNull();
        await _fixture.Scheduler.DidNotReceiveWithAnyArgs().GetTriggersOfJob(default!, default);
    }

    [Fact]
    public async Task JobToBeExecuted_SchedulerThrows_CapturesCheckInWithoutMonitorConfig()
    {
        // Arrange
        var sut = _fixture.GetSut();
        var context = _fixture.GetContext();
        _fixture.Scheduler.GetTriggersOfJob(_fixture.JobDetail.Key, Arg.Any<CancellationToken>())
            .ThrowsAsync(new SchedulerException("unavailable"));

        // Act
        await sut.JobToBeExecuted(context);

        // Assert
        _fixture.Hub.ReceivedConfigureMonitorOptions("reports-dailyemail").Should().BeNull();
    }

    [Fact]
    public async Task JobWasExecuted_JobSucceeded_CapturesOkCheckInWithDuration()
    {
        // Arrange
        var sut = _fixture.GetSut();
        var context = _fixture.GetContext();
        await sut.JobToBeExecuted(context);

        // Act
        await sut.JobWasExecuted(context, null);

        // Assert
        _fixture.Hub.Received(1).CaptureCheckIn("reports-dailyemail", CheckInStatus.Ok, _fixture.CheckInId, _fixture.JobRunTime);
    }

    [Fact]
    public async Task JobWasExecuted_JobFailed_CapturesErrorCheckInWithDuration()
    {
        // Arrange
        var sut = _fixture.GetSut();
        var context = _fixture.GetContext();
        await sut.JobToBeExecuted(context);

        // Act
        await sut.JobWasExecuted(context, new JobExecutionException("failed"));

        // Assert
        _fixture.Hub.Received(1).CaptureCheckIn("reports-dailyemail", CheckInStatus.Error, _fixture.CheckInId, _fixture.JobRunTime);
    }

    [Fact]
    public async Task JobWasExecuted_NoInProgressCheckIn_CapturesNothing()
    {
        // Arrange
        var sut = _fixture.GetSut();

        // Act
        await sut.JobWasExecuted(_fixture.GetContext(), null);

        // Assert
        _fixture.Hub.DidNotReceiveWithAnyArgs().CaptureCheckIn(default!, default);
    }

    [Fact]
    public async Task JobExecutionVetoed_CapturesNothing()
    {
        // Arrange
        var sut = _fixture.GetSut();

        // Act
        await sut.JobExecutionVetoed(_fixture.GetContext());

        // Assert
        _fixture.Hub.DidNotReceiveWithAnyArgs().CaptureCheckIn(default!, default);
    }

    [Theory]
    [InlineData("DailyEmail", "reports", null, "reports-dailyemail")]
    [InlineData("Cleanup", "DEFAULT", null, "cleanup")]
    [InlineData("Cleanup", "DEFAULT", "custom.Slug", "custom.Slug")]
    [InlineData("Cleanup", "DEFAULT", " ", "cleanup")]
    public void GetMonitorSlug_Job_ReturnsSlug(
        string name,
        string group,
        string? monitorSlug,
        string expected)
    {
        var jobBuilder = JobBuilder.Create<NoOpJob>().WithIdentity(name, group);
        if (monitorSlug is not null)
        {
            jobBuilder.UsingJobData(SentryJobListener.MonitorSlugKey, monitorSlug);
        }
        _fixture.JobDetail = jobBuilder.Build();

        SentryJobListener.GetMonitorSlug(_fixture.GetContext()).Should().Be(expected);
    }

    [Theory]
    [InlineData("Nightly Report", "nightly-report")]
    [InlineData("Billing.Invoices/Send!", "billing-invoices-send")]
    [InlineData("_queue__drain_", "queue__drain")]
    [InlineData("-a.-b-", "a-b")]
    [InlineData("日本語", null)]
    [InlineData("a234567890b234567890c234567890d234567890e234567890f234567890", "a234567890b234567890c234567890d234567890e234567890")]
    [InlineData("a234567890b234567890c234567890d234567890e23456789-f234567890", "a234567890b234567890c234567890d234567890e23456789")]
    public void ToSlug_Value_ReturnsSlugSentryLeavesUnchanged(string value, string? expected)
    {
        SentryJobListener.ToSlug(value).Should().Be(expected);
    }
}
