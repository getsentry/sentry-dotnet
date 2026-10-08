namespace Sentry.Quartz.Tests;

public class JobMonitorTests
{
    private class Fixture
    {
        public IHub Hub { get; } = Substitute.For<IHub>();
        public IScheduler Scheduler { get; } = Substitute.For<IScheduler>();
        public InMemoryDiagnosticLogger Logger { get; } = new();
        public SentryId CheckInId { get; set; } = SentryId.Create();
        public SentryQuartzOptions Options { get; } = new();
        public int TriggerCount { get; set; } = 1;

        public IJobDetail JobDetail { get; set; } = JobBuilder.Create<MonitoredJob>()
            .WithIdentity("DailyEmail", "reports")
            .Build();

        public ITrigger Trigger { get; set; } = TriggerBuilder.Create()
            .WithCronSchedule("0 0 12 * * ?", schedule => schedule.InTimeZone(TimeZoneInfo.Utc))
            .Build();

        public JobMonitor GetSut()
        {
            Hub.CaptureCheckIn(default!, default, default, default, default, default).ReturnsForAnyArgs(CheckInId);
            return new JobMonitor(Options, Hub, Logger);
        }

        public IJobExecutionContext GetContext()
        {
            var mergedJobDataMap = new JobDataMap();
            foreach (var entry in JobDetail.JobDataMap.Concat(Trigger.JobDataMap))
            {
                mergedJobDataMap[entry.Key] = entry.Value;
            }

            Scheduler.ReturnsTriggers(JobDetail.Key, Enumerable.Repeat(Trigger, TriggerCount).ToList());

            var context = Substitute.For<IJobExecutionContext>();
            context.JobDetail.Returns(JobDetail);
            context.Trigger.Returns(Trigger);
            context.Scheduler.Returns(Scheduler);
            context.MergedJobDataMap.Returns(mergedJobDataMap);
            return context;
        }

        public Task<JobCheckIn?> StartAsync() => GetSut().StartAsync(GetContext(), CancellationToken.None);
    }

    private readonly Fixture _fixture = new();

    [Fact]
    public async Task StartAsync_CronTrigger_CapturesInProgressCheckInWithMonitorConfig()
    {
        // Act
        var checkIn = await _fixture.StartAsync();

        // Assert
        checkIn!.MonitorSlug.Should().Be("reports-dailyemail");
        checkIn.Id.Should().NotBe(SentryId.Empty);
        _fixture.Hub.ReceivedInProgressCheckInId("reports-dailyemail").Should().Be(checkIn.Id);
        var monitorConfig = MonitorConfigJson.Render(_fixture.Hub.ReceivedConfigureMonitorOptions("reports-dailyemail")!);
        monitorConfig.GetProperty("schedule").GetProperty("value").GetString().Should().Be("0 12 * * *");
        monitorConfig.GetProperty("timezone").GetString().Should().Be("UTC");
    }

    [Fact]
    public async Task StartAsync_JobWithoutAttribute_CapturesNothing()
    {
        // Arrange
        _fixture.JobDetail = JobBuilder.Create<NoOpJob>().WithIdentity("DailyEmail", "reports").Build();

        // Act
        var checkIn = await _fixture.StartAsync();

        // Assert
        checkIn.Should().BeNull();
        _fixture.Hub.DidNotReceiveWithAnyArgs().CaptureCheckIn(default!, default);
    }

    [Fact]
    public async Task StartAsync_SendMonitorConfigDisabled_CapturesCheckInWithoutMonitorConfig()
    {
        // Arrange
        _fixture.Options.SendMonitorConfig = false;

        // Act
        await _fixture.StartAsync();

        // Assert
        _fixture.Hub.ReceivedConfigureMonitorOptions("reports-dailyemail").Should().BeNull();
        _fixture.Scheduler.DidNotLookUpTriggers();
    }

    [Fact]
    public async Task StartAsync_AttributeDisablesMonitorConfig_CapturesCheckInWithoutMonitorConfig()
    {
        // Arrange
        _fixture.JobDetail = JobBuilder.Create<NoMonitorConfigJob>().WithIdentity("DailyEmail", "reports").Build();

        // Act
        await _fixture.StartAsync();

        // Assert
        _fixture.Hub.ReceivedConfigureMonitorOptions("reports-dailyemail").Should().BeNull();
    }

    [Fact]
    public async Task StartAsync_ScheduleSentryCantRepresent_CapturesCheckInWithoutMonitorConfig()
    {
        // Arrange
        _fixture.Trigger = TriggerBuilder.Create()
            .WithCronSchedule("0 0 12 L * ?", schedule => schedule.InTimeZone(TimeZoneInfo.Utc))
            .Build();

        // Act
        await _fixture.StartAsync();

        // Assert
        _fixture.Hub.ReceivedConfigureMonitorOptions("reports-dailyemail").Should().BeNull();
    }

    [Fact]
    public async Task StartAsync_JobHasSeveralTriggers_CapturesCheckInWithoutMonitorConfig()
    {
        // Arrange
        _fixture.TriggerCount = 2;

        // Act
        await _fixture.StartAsync();

        // Assert
        _fixture.Hub.ReceivedConfigureMonitorOptions("reports-dailyemail").Should().BeNull();
    }

    [Fact]
    public async Task StartAsync_TriggerSetsMonitorSlug_CapturesCheckInWithThatTriggersMonitorConfig()
    {
        // Arrange
        _fixture.TriggerCount = 2;
        _fixture.Trigger = TriggerBuilder.Create()
            .WithCronSchedule("0 0 12 * * ?", schedule => schedule.InTimeZone(TimeZoneInfo.Utc))
            .UsingJobData(SentryCronMonitorSlugAttribute.JobDataKey, "daily-email-noon")
            .Build();

        // Act
        await _fixture.StartAsync();

        // Assert
        _fixture.Hub.ReceivedConfigureMonitorOptions("daily-email-noon").Should().NotBeNull();
        _fixture.Scheduler.DidNotLookUpTriggers();
    }

    [Fact]
    public async Task StartAsync_SchedulerThrows_CapturesCheckInWithoutMonitorConfig()
    {
        // Arrange
        var sut = _fixture.GetSut();
        var context = _fixture.GetContext();
        _fixture.Scheduler.TriggerLookupThrows(new SchedulerException("unavailable"));

        // Act
        await sut.StartAsync(context, CancellationToken.None);

        // Assert
        _fixture.Hub.ReceivedConfigureMonitorOptions("reports-dailyemail").Should().BeNull();
    }

    [Fact]
    public async Task StartAsync_ConfigureMonitorOptions_RunsBeforeTriggerSchedule()
    {
        // Arrange
        IJobExecutionContext? receivedContext = null;
        _fixture.Options.ConfigureMonitorOptions = (context, options) =>
        {
            receivedContext = context;
            options.FailureIssueThreshold = 3;
            options.TimeZone = "Europe/Vienna";
        };

        // Act
        await _fixture.StartAsync();

        // Assert
        var monitorConfig = MonitorConfigJson.Render(_fixture.Hub.ReceivedConfigureMonitorOptions("reports-dailyemail")!);
        receivedContext!.JobDetail.Should().BeSameAs(_fixture.JobDetail);
        monitorConfig.GetProperty("schedule").GetProperty("value").GetString().Should().Be("0 12 * * *");
        monitorConfig.GetProperty("timezone").GetString().Should().Be("Europe/Vienna");
        monitorConfig.GetProperty("failure_issue_threshold").GetInt32().Should().Be(3);
    }

    [Fact]
    public async Task StartAsync_ConfigureMonitorOptionsSetsSchedule_UsesThatSchedule()
    {
        // Arrange
        _fixture.Options.ConfigureMonitorOptions = (_, options) => options.Interval(1, SentryMonitorInterval.Day);

        // Act
        await _fixture.StartAsync();

        // Assert
        var monitorConfig = MonitorConfigJson.Render(_fixture.Hub.ReceivedConfigureMonitorOptions("reports-dailyemail")!);
        monitorConfig.GetProperty("schedule").GetProperty("type").GetString().Should().Be("interval");
        monitorConfig.TryGetProperty("timezone", out _).Should().BeFalse();
    }

    [Fact]
    public async Task StartAsync_ConfigureMonitorOptionsThrows_SendsTriggerSchedule()
    {
        // Arrange
        _fixture.Options.ConfigureMonitorOptions = (_, _) => throw new InvalidOperationException("Callback failed");

        // Act
        await _fixture.StartAsync();

        // Assert
        var monitorConfig = MonitorConfigJson.Render(_fixture.Hub.ReceivedConfigureMonitorOptions("reports-dailyemail")!);
        monitorConfig.GetProperty("schedule").GetProperty("value").GetString().Should().Be("0 12 * * *");
    }

    [Fact]
    public async Task StartAsync_ConfigureMonitorOptionsThrowsAfterSettingSchedule_SendsTriggerSchedule()
    {
        // Arrange
        _fixture.Options.ConfigureMonitorOptions = (_, options) =>
        {
            options.Interval(1, SentryMonitorInterval.Day);
            options.FailureIssueThreshold = 3;
            throw new InvalidOperationException("Callback failed");
        };

        // Act
        await _fixture.StartAsync();

        // Assert
        var monitorConfig = MonitorConfigJson.Render(_fixture.Hub.ReceivedConfigureMonitorOptions("reports-dailyemail")!);
        monitorConfig.GetProperty("schedule").GetProperty("value").GetString().Should().Be("0 12 * * *");
        monitorConfig.TryGetProperty("failure_issue_threshold", out _).Should().BeFalse();
    }

    [Fact]
    public async Task StartAsync_InProgressCheckInNotCaptured_KeepsItsId()
    {
        // Arrange
        _fixture.CheckInId = SentryId.Empty;

        // Act
        var checkIn = await _fixture.StartAsync();

        // Assert
        checkIn!.Id.Should().NotBe(SentryId.Empty);
        _fixture.Hub.ReceivedInProgressCheckInId("reports-dailyemail").Should().Be(checkIn.Id);
    }

    [Fact]
    public async Task StartAsync_InProgressCheckInThrows_StillReturnsCheckIn()
    {
        // Arrange
        var sut = _fixture.GetSut();
        _fixture.Hub.CaptureCheckIn(default!, default, default, default, default, default)
            .ThrowsForAnyArgs(new InvalidOperationException("Hub failed"));

        // Act
        var checkIn = await sut.StartAsync(_fixture.GetContext(), CancellationToken.None);

        // Assert
        checkIn!.Id.Should().NotBe(SentryId.Empty);
    }

    [Fact]
    public async Task StartAsync_ConfigureMonitorOptionsWithUnconvertibleTrigger_RunsCallback()
    {
        // Arrange
        _fixture.Trigger = TriggerBuilder.Create()
            .WithCronSchedule("0 0 12 L * ?", schedule => schedule.InTimeZone(TimeZoneInfo.Utc))
            .Build();
        var called = false;
        _fixture.Options.ConfigureMonitorOptions = (_, _) => called = true;

        // Act
        await _fixture.StartAsync();

        // Assert
        called.Should().BeTrue();
        _fixture.Hub.ReceivedConfigureMonitorOptions("reports-dailyemail").Should().BeNull();
    }

    [Fact]
    public async Task StartAsync_ConfigureMonitorOptionsSetsScheduleForUnconvertibleTrigger_SendsThatSchedule()
    {
        // Arrange
        _fixture.Trigger = TriggerBuilder.Create()
            .WithCronSchedule("0 0 12 L * ?", schedule => schedule.InTimeZone(TimeZoneInfo.Utc))
            .Build();
        _fixture.Options.ConfigureMonitorOptions = (_, options) =>
        {
            options.Interval("0 12 28-31 * *");
            options.TimeZone = "Europe/Vienna";
        };

        // Act
        await _fixture.StartAsync();

        // Assert
        var monitorConfig = MonitorConfigJson.Render(_fixture.Hub.ReceivedConfigureMonitorOptions("reports-dailyemail")!);
        monitorConfig.GetProperty("schedule").GetProperty("value").GetString().Should().Be("0 12 28-31 * *");
        monitorConfig.GetProperty("timezone").GetString().Should().Be("Europe/Vienna");
    }

    [Fact]
    public async Task StartAsync_ConfigureMonitorOptionsWithoutAnySchedule_CapturesCheckInWithoutMonitorConfig()
    {
        // Arrange
        _fixture.Trigger = TriggerBuilder.Create()
            .WithCronSchedule("0 0 12 L * ?", schedule => schedule.InTimeZone(TimeZoneInfo.Utc))
            .Build();
        _fixture.Options.ConfigureMonitorOptions = (_, options) =>
        {
            options.FailureIssueThreshold = 3;
            options.RecoveryThreshold = 2;
        };

        // Act
        await _fixture.StartAsync();

        // Assert
        _fixture.Hub.ReceivedConfigureMonitorOptions("reports-dailyemail").Should().BeNull();
    }

    [Fact]
    public async Task StartAsync_SendMonitorConfigDisabled_DoesNotRunCallback()
    {
        // Arrange
        _fixture.Options.SendMonitorConfig = false;
        var called = false;
        _fixture.Options.ConfigureMonitorOptions = (_, _) => called = true;

        // Act
        await _fixture.StartAsync();

        // Assert
        called.Should().BeFalse();
    }

    [Fact]
    public async Task StartAsync_JobWithoutIdentity_UsesClassNameAndLogs()
    {
        // Arrange
        var sut = _fixture.GetSut();
        _fixture.JobDetail = JobBuilder.Create<MonitoredJob>().Build();
        var first = _fixture.GetContext();
        _fixture.JobDetail = JobBuilder.Create<MonitoredJob>().Build();
        var second = _fixture.GetContext();

        // Act
        var firstCheckIn = await sut.StartAsync(first, CancellationToken.None);
        var secondCheckIn = await sut.StartAsync(second, CancellationToken.None);

        // Assert
        firstCheckIn!.MonitorSlug.Should().Be("monitoredjob");
        secondCheckIn!.MonitorSlug.Should().Be("monitoredjob");
        var entries = _fixture.Logger.Entries.Where(e => e.Message.Contains("has no identity")).ToList();
        entries.Should().HaveCount(2).And.OnlyContain(e => e.Level == SentryLevel.Debug);
        string.Format(entries[0].Message, entries[0].Args).Should().Be(
            $"Job `{typeof(MonitoredJob).FullName}` has no identity, so its monitor slug is `monitoredjob`. Give the job " +
            "an identity (WithIdentity(...)) or set a slug ([SentryCronMonitorSlug(\"…\")]) to keep the monitor stable.");
    }

    [Fact]
    public void Finish_HubThrows_DoesNotThrow()
    {
        // Arrange
        var sut = _fixture.GetSut();
        _fixture.Hub.CaptureCheckIn(default!, default, default, default, default, default)
            .ThrowsForAnyArgs(new InvalidOperationException("Hub failed"));

        // Act
        var act = () => sut.Finish(new JobCheckIn("reports-dailyemail", SentryId.Create()), failed: false, TimeSpan.Zero);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Finish_Failed_CapturesErrorCheckInWithDuration()
    {
        // Arrange
        var sut = _fixture.GetSut();
        var duration = TimeSpan.FromSeconds(3);

        // Act
        sut.Finish(new JobCheckIn("reports-dailyemail", _fixture.CheckInId), failed: true, duration);

        // Assert
        _fixture.Hub.Received(1).CaptureCheckIn("reports-dailyemail", CheckInStatus.Error, _fixture.CheckInId, duration);
    }

    [Fact]
    public void Finish_Succeeded_CapturesOkCheckInWithDuration()
    {
        // Arrange
        var sut = _fixture.GetSut();
        var duration = TimeSpan.FromSeconds(3);

        // Act
        sut.Finish(new JobCheckIn("reports-dailyemail", _fixture.CheckInId), failed: false, duration);

        // Assert
        _fixture.Hub.Received(1).CaptureCheckIn("reports-dailyemail", CheckInStatus.Ok, _fixture.CheckInId, duration);
    }

    [Theory]
    [InlineData(typeof(MonitoredJob), "DailyEmail", "reports", null, "reports-dailyemail")]
    [InlineData(typeof(MonitoredJob), "Cleanup", "DEFAULT", null, "cleanup")]
    [InlineData(typeof(MonitoredJob), "Cleanup", "DEFAULT", "custom.Slug", "custom.Slug")]
    [InlineData(typeof(MonitoredJob), "Cleanup", "DEFAULT", " ", "cleanup")]
    [InlineData(typeof(CustomSlugJob), "Cleanup", "DEFAULT", null, "custom-slug")]
    [InlineData(typeof(CustomSlugJob), "Cleanup", "DEFAULT", "data-map-slug", "data-map-slug")]
    [InlineData(typeof(MonitoredJob), "3f2504e0-4f89-11d3-9a0c-0305e82c3301", "DEFAULT", null, "monitoredjob")]
    [InlineData(typeof(MonitoredJob), "3f2504e0-4f89-11d3-9a0c-0305e82c3301", "reports", null, "monitoredjob")]
    [InlineData(typeof(CustomSlugJob), "3f2504e0-4f89-11d3-9a0c-0305e82c3301", "DEFAULT", null, "custom-slug")]
    public void GetMonitorSlug_Job_ReturnsSlug(
        Type jobType,
        string name,
        string group,
        string? dataMapSlug,
        string expected)
    {
        var jobBuilder = JobBuilder.Create().OfType(jobType).WithIdentity(name, group);
        if (dataMapSlug is not null)
        {
            jobBuilder.UsingJobData(SentryCronMonitorSlugAttribute.JobDataKey, dataMapSlug);
        }
        _fixture.JobDetail = jobBuilder.Build();
        var attribute = jobType.GetCustomAttribute<SentryCronMonitorSlugAttribute>()!;

        JobMonitor.GetMonitorSlug(_fixture.GetContext(), attribute).Should().Be(expected);
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
        JobMonitor.ToSlug(value).Should().Be(expected);
    }
}
