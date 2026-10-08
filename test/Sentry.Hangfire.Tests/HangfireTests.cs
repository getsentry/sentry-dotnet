using Hangfire;

namespace Sentry.Hangfire.Tests;

public class HangfireTests : IClassFixture<HangfireFixture>
{
    private readonly HangfireFixture _fixture;

    public HangfireTests(HangfireFixture hangfireFixture)
    {
        _fixture = hangfireFixture;
    }

    [Fact]
    public async Task ExecuteJobWithAttribute_CapturesCheckInInProgressAndOkWithDuration()
    {
        await _fixture.Enqueue<TestJob>(job => job.ExecuteJobWithAttribute());

        _fixture.Hub.Received(1).CaptureCheckIn(
            Arg.Is<string>("test-job"),
            Arg.Is<CheckInStatus>(status => status == CheckInStatus.InProgress),
            Arg.Any<SentryId?>());
        var checkInId = _fixture.Hub.ReceivedInProgressCheckInId("test-job");
        checkInId.Should().NotBeNull().And.NotBe(SentryId.Empty);
        _fixture.Hub.Received(1).CaptureCheckIn(
            Arg.Is<string>("test-job"),
            Arg.Is<CheckInStatus>(status => status == CheckInStatus.Ok),
            checkInId,
            Arg.Is<TimeSpan?>(duration => duration != null), Arg.Any<Scope>());
    }

    [Fact]
    public async Task ExecuteJobWithException_CapturesCheckInInProgressAndErrorWithDuration()
    {
        await _fixture.Enqueue<TestJob>(job => job.ExecuteJobWithException());

        await Task.Delay(1000);

        _fixture.Hub.Received(1).CaptureCheckIn(
            Arg.Is<string>("test-job-with-exception"),
            Arg.Is<CheckInStatus>(status => status == CheckInStatus.InProgress),
            Arg.Any<SentryId?>());
        var checkInId = _fixture.Hub.ReceivedInProgressCheckInId("test-job-with-exception");
        checkInId.Should().NotBeNull().And.NotBe(SentryId.Empty);
        _fixture.Hub.Received(1).CaptureCheckIn(
            Arg.Is<string>("test-job-with-exception"),
            Arg.Is<CheckInStatus>(status => status == CheckInStatus.Error),
            checkInId,
            Arg.Is<TimeSpan?>(duration => duration != null), Arg.Any<Scope>());
    }

    [Fact]
    public async Task ExecuteJobWithoutAttribute_DoesNotCapturesCheckInButLogs()
    {
        var sentryId = SentryId.Create();
        _fixture.Hub.CaptureCheckIn(Arg.Any<string>(), Arg.Any<CheckInStatus>()).Returns(sentryId);

        await _fixture.Enqueue<TestJob>(job => job.ExecuteJobWithoutAttribute());

        _fixture.Hub.DidNotReceive().CaptureCheckIn(Arg.Any<string>(), Arg.Any<CheckInStatus>(), Arg.Is<SentryId>(id => id == sentryId));
        _fixture.Logger.Received(1).Log(SentryLevel.Debug, Arg.Is<string>(message => message.Contains("Skipping creating a check-in for")), null, Arg.Any<Type>(), Arg.Any<MethodInfo>());
    }

    [SkippableFact]
    public async Task ExecuteRecurringJob_SendScheduleEnabled_CapturesCheckInWithMonitorConfig()
    {
        Skip.If(!TestEnvironment.HasTimeZone("Europe/Berlin"), "No time zone database");

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

        await _fixture.TriggerRecurringJob<TestJob>("test-recurring-job-id", job => job.ExecuteRecurringJob(), "30 0 0 1 1 *", timeZone);

        var configureMonitorOptions = _fixture.Hub.ReceivedConfigureMonitorOptions("test-recurring-job");
        configureMonitorOptions.Should().NotBeNull();
        var monitorConfig = MonitorConfigJson.Render(configureMonitorOptions!);
        monitorConfig.GetProperty("schedule").GetProperty("type").GetString().Should().Be("crontab");
        monitorConfig.GetProperty("schedule").GetProperty("value").GetString().Should().Be("0 0 1 1 *");
        monitorConfig.GetProperty("timezone").GetString().Should().Be("Europe/Berlin");
    }

    // In this class so it runs serially with the fixture's jobs, which would also pick up the filter it adds
    [Fact]
    public void UseSentry_ConfigureOptions_AddsFilterWithOptions()
    {
        var configuration = Substitute.For<IGlobalConfiguration>();
        var existingFilters = SentryFilters().ToList();

        configuration.UseSentry(options => options.SendRecurringJobSchedule = false);

        var addedFilters = SentryFilters().Except(existingFilters).ToList();
        foreach (var filter in addedFilters)
        {
            GlobalJobFilters.Filters.Remove(filter);
        }
        addedFilters.Should().ContainSingle().Which.Options.SendRecurringJobSchedule.Should().BeFalse();

        static IEnumerable<SentryServerFilter> SentryFilters() =>
            GlobalJobFilters.Filters.Select(filter => filter.Instance).OfType<SentryServerFilter>();
    }

    [Fact]
    public void UseSentry_NullConfigureOptions_Throws()
    {
        var configuration = Substitute.For<IGlobalConfiguration>();

        var act = () => configuration.UseSentry((Action<SentryHangfireOptions>)null!);

        act.Should().Throw<ArgumentNullException>();
    }
}

public class TestJob
{
    [SentryMonitorSlug("test-job")]
    public void ExecuteJobWithAttribute()
    { }

    [SentryMonitorSlug("test-job-with-exception")]
    public void ExecuteJobWithException()
    {
        throw new Exception();
    }

    public void ExecuteJobWithoutAttribute()
    { }

    [SentryMonitorSlug("test-recurring-job")]
    public void ExecuteRecurringJob()
    { }
}
