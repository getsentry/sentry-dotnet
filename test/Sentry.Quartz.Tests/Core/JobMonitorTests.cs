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
        public ISystemClock Clock { get; } = Substitute.For<ISystemClock>();
        public DateTimeOffset Now { get; } = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        public List<(IJobDetail Job, ITrigger Trigger)> OtherTriggers { get; } = [];

        public IJobDetail JobDetail { get; set; } = JobBuilder.Create<MonitoredJob>()
            .WithIdentity("DailyEmail", "reports")
            .Build();

        public ITrigger Trigger { get; set; } = TriggerBuilder.Create()
            .WithCronSchedule("0 0 12 * * ?", schedule => schedule.InTimeZone(TimeZoneInfo.Utc))
            .Build();

        public JobMonitor GetSut()
        {
            Hub.CaptureCheckIn(default!, default, default, default, default, default).ReturnsForAnyArgs(CheckInId);
            Clock.GetUtcNow().Returns(Now);
            return new JobMonitor(Options, Hub, Logger, Clock);
        }

        public IJobExecutionContext GetContext()
        {
            if (!JobDetail.Key.Equals(Trigger.JobKey))
            {
                Trigger = Trigger.GetTriggerBuilder().ForJob(JobDetail).Build();
            }

            var mergedJobDataMap = new JobDataMap();
            foreach (var entry in JobDetail.JobDataMap.Concat(Trigger.JobDataMap))
            {
                mergedJobDataMap[entry.Key] = entry.Value;
            }

            Scheduler.ReturnsTriggers([(JobDetail, Trigger), .. OtherTriggers]);

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
    public async Task StartAsync_JobHasTriggersWithDifferentSchedules_CapturesCheckInWithoutMonitorConfig()
    {
        // Arrange
        _fixture.OtherTriggers.Add((_fixture.JobDetail, CronTrigger("0 0 18 * * ?", _fixture.JobDetail)));

        // Act
        await _fixture.StartAsync();

        // Assert
        _fixture.Hub.ReceivedConfigureMonitorOptions("reports-dailyemail").Should().BeNull();
        _fixture.Logger.Entries.Should().Contain(e => e.Message.Contains("with a different schedule"));
    }

    [Fact]
    public async Task StartAsync_JobHasTriggersWithSameSchedule_CapturesCheckInWithMonitorConfig()
    {
        // Arrange
        _fixture.OtherTriggers.Add((_fixture.JobDetail, CronTrigger("0 0 12 * * ?", _fixture.JobDetail)));

        // Act
        await _fixture.StartAsync();

        // Assert
        _fixture.Hub.ReceivedConfigureMonitorOptions("reports-dailyemail").Should().NotBeNull();
    }

    [Fact]
    public async Task StartAsync_JobsShareSlugWithDifferentSchedules_CapturesCheckInWithoutMonitorConfig()
    {
        // Arrange
        _fixture.JobDetail = JobBuilder.Create<CustomSlugJob>().WithIdentity("eu", "reports").Build();
        var other = JobBuilder.Create<CustomSlugJob>().WithIdentity("us", "reports").Build();
        _fixture.OtherTriggers.Add((other, CronTrigger("0 0 18 * * ?", other)));

        // Act
        await _fixture.StartAsync();

        // Assert
        _fixture.Hub.ReceivedConfigureMonitorOptions("custom-slug").Should().BeNull();
    }

    [Fact]
    public async Task StartAsync_JobsShareSlugWithDifferentTimeZones_CapturesCheckInWithoutMonitorConfig()
    {
        // Arrange
        _fixture.JobDetail = JobBuilder.Create<CustomSlugJob>().WithIdentity("eu", "reports").Build();
        var other = JobBuilder.Create<CustomSlugJob>().WithIdentity("us", "reports").Build();
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        _fixture.OtherTriggers.Add((other, CronTrigger("0 0 12 * * ?", other, timeZone)));

        // Act
        await _fixture.StartAsync();

        // Assert
        _fixture.Hub.ReceivedConfigureMonitorOptions("custom-slug").Should().BeNull();
    }

    [Fact]
    public async Task StartAsync_JobsShareSlugWithSameConvertedSchedule_CapturesCheckInWithMonitorConfig()
    {
        // Arrange
        _fixture.JobDetail = JobBuilder.Create<CustomSlugJob>().WithIdentity("eu", "reports").Build();
        var other = JobBuilder.Create<CustomSlugJob>().WithIdentity("us", "reports").Build();
        _fixture.OtherTriggers.Add((other, CronTrigger("0 0 12 ? * *", other)));

        // Act
        await _fixture.StartAsync();

        // Assert
        _fixture.Hub.ReceivedConfigureMonitorOptions("custom-slug").Should().NotBeNull();
    }

    [Fact]
    public async Task StartAsync_IdentitylessJobsWithDifferentSchedules_CapturesCheckInWithoutMonitorConfig()
    {
        // Arrange
        _fixture.JobDetail = JobBuilder.Create<MonitoredJob>().Build();
        var other = JobBuilder.Create<MonitoredJob>().Build();
        _fixture.OtherTriggers.Add((other, CronTrigger("0 0 18 * * ?", other)));

        // Act
        await _fixture.StartAsync();

        // Assert
        _fixture.Hub.ReceivedConfigureMonitorOptions("monitoredjob").Should().BeNull();
    }

    [Fact]
    public async Task StartAsync_OtherTriggerSetsItsOwnSlug_CapturesCheckInWithMonitorConfig()
    {
        // Arrange
        _fixture.OtherTriggers.Add((_fixture.JobDetail, CronTrigger("0 0 18 * * ?", _fixture.JobDetail,
            monitorSlug: "daily-email-evening")));

        // Act
        await _fixture.StartAsync();

        // Assert
        _fixture.Hub.ReceivedConfigureMonitorOptions("reports-dailyemail").Should().NotBeNull();
    }

    [Fact]
    public async Task StartAsync_OtherJobWithoutAttributeSharesSlug_CapturesCheckInWithMonitorConfig()
    {
        // Arrange
        var other = JobBuilder.Create<NoOpJob>().WithIdentity("DailyEmail", "Reports").Build();
        _fixture.OtherTriggers.Add((other, CronTrigger("0 0 18 * * ?", other)));

        // Act
        await _fixture.StartAsync();

        // Assert
        _fixture.Hub.ReceivedConfigureMonitorOptions("reports-dailyemail").Should().NotBeNull();
    }

    [Fact]
    public async Task StartAsync_OtherTriggerScheduleSentryCantRepresent_CapturesCheckInWithMonitorConfig()
    {
        // Arrange
        _fixture.OtherTriggers.Add((_fixture.JobDetail, CronTrigger("0 0 12 L * ?", _fixture.JobDetail)));

        // Act
        await _fixture.StartAsync();

        // Assert
        _fixture.Hub.ReceivedConfigureMonitorOptions("reports-dailyemail").Should().NotBeNull();
    }

    [Fact]
    public async Task StartAsync_TriggerSetsMonitorSlug_CapturesCheckInWithThatTriggersMonitorConfig()
    {
        // Arrange
        _fixture.Trigger = CronTrigger("0 0 12 * * ?", monitorSlug: "daily-email-noon");
        _fixture.OtherTriggers.Add((_fixture.JobDetail, CronTrigger("0 0 18 * * ?", _fixture.JobDetail)));

        // Act
        await _fixture.StartAsync();

        // Assert
        _fixture.Hub.ReceivedConfigureMonitorOptions("daily-email-noon").Should().NotBeNull();
    }

    [Fact]
    public async Task StartAsync_WithinTtl_LooksUpTriggersOnce()
    {
        // Arrange
        var sut = _fixture.GetSut();
        var context = _fixture.GetContext();

        // Act
        await sut.StartAsync(context, CancellationToken.None);
        _fixture.Clock.GetUtcNow().Returns(_fixture.Now + JobMonitor.TriggerSchedulesTtl - TimeSpan.FromSeconds(1));
        await sut.StartAsync(context, CancellationToken.None);

        // Assert
        _fixture.Scheduler.LookedUpTriggers(1);
    }

    [Fact]
    public async Task StartAsync_AfterTtl_LooksUpTriggersAgain()
    {
        // Arrange
        var sut = _fixture.GetSut();
        var context = _fixture.GetContext();

        // Act
        await sut.StartAsync(context, CancellationToken.None);
        _fixture.Clock.GetUtcNow().Returns(_fixture.Now + JobMonitor.TriggerSchedulesTtl);
        await sut.StartAsync(context, CancellationToken.None);

        // Assert
        _fixture.Scheduler.LookedUpTriggers(2);
    }

    [Fact]
    public async Task StartAsync_ConflictingTriggerAddedAfterTtl_CapturesCheckInWithoutMonitorConfig()
    {
        // Arrange
        var sut = _fixture.GetSut();
        await sut.StartAsync(_fixture.GetContext(), CancellationToken.None);
        _fixture.OtherTriggers.Add((_fixture.JobDetail, CronTrigger("0 0 18 * * ?", _fixture.JobDetail)));
        var context = _fixture.GetContext();
        _fixture.Hub.ClearReceivedCalls();

        // Act
        await sut.StartAsync(context, CancellationToken.None);
        var withinTtl = _fixture.Hub.ReceivedConfigureMonitorOptions("reports-dailyemail");
        _fixture.Hub.ClearReceivedCalls();
        _fixture.Clock.GetUtcNow().Returns(_fixture.Now + JobMonitor.TriggerSchedulesTtl);
        await sut.StartAsync(context, CancellationToken.None);

        // Assert
        withinTtl.Should().NotBeNull();
        _fixture.Hub.ReceivedConfigureMonitorOptions("reports-dailyemail").Should().BeNull();
    }

    [Fact]
    public async Task StartAsync_TriggerNotInLookup_LooksUpTriggersAgain()
    {
        // Arrange
        var sut = _fixture.GetSut();
        await sut.StartAsync(_fixture.GetContext(), CancellationToken.None);
        _fixture.Trigger = CronTrigger("0 0 12 * * ?");

        // Act
        await sut.StartAsync(_fixture.GetContext(), CancellationToken.None);

        // Assert
        _fixture.Scheduler.LookedUpTriggers(2);
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
            "an identity (WithIdentity(...)) or set a slug ([SentryMonitorSlug(\"…\")]) to keep the monitor stable.");
    }

    [Fact]
    public async Task StartAsync_MonitorAllJobsWithoutAttribute_CapturesCheckInWithMonitorConfig()
    {
        // Arrange
        _fixture.Options.MonitorAllJobs = true;
        _fixture.JobDetail = JobBuilder.Create<NoOpJob>().WithIdentity("DailyEmail", "reports").Build();

        // Act
        var checkIn = await _fixture.StartAsync();

        // Assert
        checkIn!.MonitorSlug.Should().Be("reports-dailyemail");
        var monitorConfig = MonitorConfigJson.Render(_fixture.Hub.ReceivedConfigureMonitorOptions("reports-dailyemail")!);
        monitorConfig.GetProperty("schedule").GetProperty("value").GetString().Should().Be("0 12 * * *");
    }

    [Fact]
    public async Task StartAsync_MonitorAllJobsDefaultGroup_UsesJobName()
    {
        // Arrange
        _fixture.Options.MonitorAllJobs = true;
        _fixture.JobDetail = JobBuilder.Create<NoOpJob>().WithIdentity("Nightly Cleanup").Build();

        // Act
        var checkIn = await _fixture.StartAsync();

        // Assert
        checkIn!.MonitorSlug.Should().Be("nightly-cleanup");
    }

    [Fact]
    public async Task StartAsync_MonitorAllJobsRepeatingSimpleTrigger_CapturesCheckIn()
    {
        // Arrange
        _fixture.Options.MonitorAllJobs = true;
        _fixture.JobDetail = JobBuilder.Create<NoOpJob>().WithIdentity("DailyEmail", "reports").Build();
        _fixture.Trigger = TriggerBuilder.Create()
            .WithSimpleSchedule(schedule => schedule.WithInterval(TimeSpan.FromMinutes(15)).RepeatForever())
            .Build();

        // Act
        var checkIn = await _fixture.StartAsync();

        // Assert
        checkIn!.MonitorSlug.Should().Be("reports-dailyemail");
        _fixture.Hub.ReceivedConfigureMonitorOptions("reports-dailyemail").Should().NotBeNull();
    }

    [Fact]
    public async Task StartAsync_MonitorAllJobsOneOffTrigger_CapturesNothing()
    {
        // Arrange
        _fixture.Options.MonitorAllJobs = true;
        _fixture.JobDetail = JobBuilder.Create<NoOpJob>().WithIdentity("DailyEmail", "reports").Build();
        _fixture.Trigger = TriggerBuilder.Create().StartNow().Build();

        // Act
        var checkIn = await _fixture.StartAsync();

        // Assert
        checkIn.Should().BeNull();
        _fixture.Hub.DidNotReceiveWithAnyArgs().CaptureCheckIn(default!, default);
    }

    [Fact]
    public async Task StartAsync_MonitorAllJobsWithoutIdentity_CapturesNothing()
    {
        // Arrange
        _fixture.Options.MonitorAllJobs = true;
        _fixture.JobDetail = JobBuilder.Create<NoOpJob>().Build();

        // Act
        var checkIn = await _fixture.StartAsync();

        // Assert
        checkIn.Should().BeNull();
    }

    [Theory]
    [InlineData("DailyEmail")]
    [InlineData("reports.DailyEmail")]
    public async Task StartAsync_MonitorAllJobsExcludedByString_CapturesNothing(string excluded)
    {
        // Arrange
        _fixture.Options.MonitorAllJobs = true;
        _fixture.Options.ExcludedJobs.Add(excluded);
        _fixture.JobDetail = JobBuilder.Create<NoOpJob>().WithIdentity("DailyEmail", "reports").Build();

        // Act
        var checkIn = await _fixture.StartAsync();

        // Assert
        checkIn.Should().BeNull();
        _fixture.Logger.Entries.Should().Contain(e => e.Message.Contains("ExcludedJobs"));
    }

    [Theory]
    [InlineData("Daily")]
    [InlineData("reports")]
    [InlineData("reports.DailyEmail.Other")]
    public async Task StartAsync_MonitorAllJobsStringOnlyPartlyMatches_CapturesCheckIn(string excluded)
    {
        // Arrange
        _fixture.Options.MonitorAllJobs = true;
        _fixture.Options.ExcludedJobs.Add(excluded);
        _fixture.JobDetail = JobBuilder.Create<NoOpJob>().WithIdentity("DailyEmail", "reports").Build();

        // Act
        var checkIn = await _fixture.StartAsync();

        // Assert
        checkIn.Should().NotBeNull();
    }

    [Fact]
    public async Task StartAsync_MonitorAllJobsExcludedByRegex_CapturesNothing()
    {
        // Arrange
        _fixture.Options.MonitorAllJobs = true;
        _fixture.Options.ExcludedJobs.Add(new Regex("^reports\\."));
        _fixture.JobDetail = JobBuilder.Create<NoOpJob>().WithIdentity("DailyEmail", "reports").Build();

        // Act
        var checkIn = await _fixture.StartAsync();

        // Assert
        checkIn.Should().BeNull();
    }

    [Fact]
    public async Task StartAsync_ExcludedJobWithAttribute_CapturesCheckIn()
    {
        // Arrange
        _fixture.Options.MonitorAllJobs = true;
        _fixture.Options.ExcludedJobs.Add("reports.DailyEmail");

        // Act
        var checkIn = await _fixture.StartAsync();

        // Assert
        checkIn!.MonitorSlug.Should().Be("reports-dailyemail");
    }

    [Fact]
    public async Task StartAsync_MonitorAllJobsDataMapSlug_UsesThatSlug()
    {
        // Arrange
        _fixture.Options.MonitorAllJobs = true;
        _fixture.JobDetail = JobBuilder.Create<NoOpJob>().WithIdentity("DailyEmail", "reports")
            .UsingJobData(SentryMonitorSlugAttribute.JobDataKey, "daily-email").Build();

        // Act
        var checkIn = await _fixture.StartAsync();

        // Assert
        checkIn!.MonitorSlug.Should().Be("daily-email");
    }

    [Fact]
    public async Task StartAsync_MonitorAllJobsJobHasTriggersWithDifferentSchedules_CapturesCheckInWithoutMonitorConfig()
    {
        // Arrange
        _fixture.Options.MonitorAllJobs = true;
        _fixture.JobDetail = JobBuilder.Create<NoOpJob>().WithIdentity("DailyEmail", "reports").Build();
        _fixture.OtherTriggers.Add((_fixture.JobDetail, CronTrigger("0 0 18 * * ?", _fixture.JobDetail)));

        // Act
        await _fixture.StartAsync();

        // Assert
        _fixture.Hub.ReceivedConfigureMonitorOptions("reports-dailyemail").Should().BeNull();
    }

    [Fact]
    public async Task StartAsync_MonitorAllJobsOtherJobWithoutAttributeSharesSlug_CapturesCheckInWithoutMonitorConfig()
    {
        // Arrange
        _fixture.Options.MonitorAllJobs = true;
        var other = JobBuilder.Create<NoOpJob>().WithIdentity("DailyEmail", "Reports").Build();
        _fixture.OtherTriggers.Add((other, CronTrigger("0 0 18 * * ?", other)));

        // Act
        await _fixture.StartAsync();

        // Assert
        _fixture.Hub.ReceivedConfigureMonitorOptions("reports-dailyemail").Should().BeNull();
    }

    [Fact]
    public async Task StartAsync_MonitorAllJobsExcludedJobSharesSlug_CapturesCheckInWithMonitorConfig()
    {
        // Arrange
        _fixture.Options.MonitorAllJobs = true;
        _fixture.Options.ExcludedJobs.Add("Reports.DailyEmail");
        var other = JobBuilder.Create<NoOpJob>().WithIdentity("DailyEmail", "Reports").Build();
        _fixture.OtherTriggers.Add((other, CronTrigger("0 0 18 * * ?", other)));

        // Act
        await _fixture.StartAsync();

        // Assert
        _fixture.Hub.ReceivedConfigureMonitorOptions("reports-dailyemail").Should().NotBeNull();
    }

    [Fact]
    public async Task StartAsync_MonitorAllJobsWithinTtl_LooksUpTriggersOnce()
    {
        // Arrange
        _fixture.Options.MonitorAllJobs = true;
        _fixture.JobDetail = JobBuilder.Create<NoOpJob>().WithIdentity("DailyEmail", "reports").Build();
        var sut = _fixture.GetSut();
        var context = _fixture.GetContext();

        // Act
        await sut.StartAsync(context, CancellationToken.None);
        await sut.StartAsync(context, CancellationToken.None);

        // Assert
        _fixture.Scheduler.LookedUpTriggers(1);
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
            jobBuilder.UsingJobData(SentryMonitorSlugAttribute.JobDataKey, dataMapSlug);
        }
        _fixture.JobDetail = jobBuilder.Build();
        var attribute = jobType.GetCustomAttribute<SentryMonitorSlugAttribute>()!;

        JobMonitor.GetMonitorSlug(_fixture.GetContext(), attribute).Should().Be(expected);
    }

    private static ITrigger CronTrigger(
        string cronExpression,
        IJobDetail? job = null,
        TimeZoneInfo? timeZone = null,
        string? monitorSlug = null)
    {
        var builder = TriggerBuilder.Create()
            .WithCronSchedule(cronExpression, schedule => schedule.InTimeZone(timeZone ?? TimeZoneInfo.Utc));
        if (job is not null)
        {
            builder.ForJob(job);
        }

        if (monitorSlug is not null)
        {
            builder.UsingJobData(SentryMonitorSlugAttribute.JobDataKey, monitorSlug);
        }

        return builder.Build();
    }
}
