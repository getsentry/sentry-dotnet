using Hangfire;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.Storage;
using Sentry.Infrastructure;

namespace Sentry.Hangfire.Tests;

public class ServerFilterTests
{
    [Fact]
    public void OnPerforming_IsReentrant()
    {
        // Arrange
        const string jobId = "test-id";
        const string monitorSlug = "test-monitor-slug";

        var storageConnection = Substitute.For<IStorageConnection>();
        storageConnection.GetJobParameter(jobId, SentryServerFilter.SentryMonitorSlugKey).Returns(
            SerializationHelper.Serialize(monitorSlug)
            );

        var backgroundJob = new BackgroundJob(jobId, null, DateTime.Now);
        var cancellationToken = Substitute.For<IJobCancellationToken>();
        var performContext = new PerformContext(
            null,
            storageConnection,
            backgroundJob,
            cancellationToken
            );
        var performingContext = new PerformingContext(performContext);

        var hub = Substitute.For<IHub>();

        var logger = Substitute.For<IDiagnosticLogger>();
        var filter = new SentryServerFilter(hub, logger);

        // Act
        filter.OnPerforming(performingContext);

        // Assert
        performContext.Items.ContainsKey(SentryServerFilter.SentryCheckInIdKey).Should().BeTrue();
        var firstKey = performingContext.Items[SentryServerFilter.SentryCheckInIdKey];

        // Act
        filter.OnPerforming(performingContext);

        // Assert
        performContext.Items.ContainsKey(SentryServerFilter.SentryCheckInIdKey).Should().BeTrue();
        performingContext.Items[SentryServerFilter.SentryCheckInIdKey].Should().NotBe(firstKey);
    }

    [Fact]
    public void OnPerformed_InProgressCheckInNotCaptured_FinishesWithSameNonEmptyId()
    {
        // Arrange
        var fixture = new RecurringJobFixture { CheckInId = SentryId.Empty };
        var filter = fixture.GetSut();
        filter.OnPerforming(fixture.PerformingContext);
        var performedContext = new PerformedContext(fixture.PerformingContext, null, false, null);

        // Act
        filter.OnPerformed(performedContext);

        // Assert
        var checkInId = fixture.Hub.ReceivedInProgressCheckInId(RecurringJobFixture.MonitorSlug);
        checkInId.Should().NotBeNull().And.NotBe(SentryId.Empty);
        fixture.Hub.Received(1).CaptureCheckIn(RecurringJobFixture.MonitorSlug, CheckInStatus.Ok, checkInId, Arg.Any<TimeSpan?>());
    }

    [SkippableTheory]
    [InlineData("0 */2 * * *", "Europe/Berlin", "0 */2 * * *", "Europe/Berlin")]
    [InlineData("30 0 */2 * * *", "UTC", "0 */2 * * *", "UTC")]
    [InlineData("0 0 * * MON", null, "0 0 * * MON", "UTC")]
    public void OnPerforming_RecurringJobWithSendMonitorConfigEnabled_SendsMonitorConfig(
        string cron, string? timeZoneId, string expectedCrontab, string expectedTimeZone)
    {
        Skip.If(timeZoneId is not (null or "UTC") && !TestEnvironment.HasTimeZone(timeZoneId), "No time zone database");

        // Arrange
        var fixture = new RecurringJobFixture { Cron = cron, TimeZoneId = timeZoneId };
        var filter = fixture.GetSut(sendMonitorConfig: true);

        // Act
        filter.OnPerforming(fixture.PerformingContext);

        // Assert
        var monitorConfig = fixture.GetSentMonitorConfig();
        monitorConfig.Should().NotBeNull();
        monitorConfig!.Value.GetProperty("schedule").GetProperty("type").GetString().Should().Be("crontab");
        monitorConfig.Value.GetProperty("schedule").GetProperty("value").GetString().Should().Be(expectedCrontab);
        monitorConfig.Value.GetProperty("timezone").GetString().Should().Be(expectedTimeZone);
        fixture.PerformingContext.Items[SentryServerFilter.SentryCheckInIdKey].Should().Be(fixture.Hub.ReceivedInProgressCheckInId(RecurringJobFixture.MonitorSlug));
    }

    [Fact]
    public void OnPerforming_DefaultOptions_SendsMonitorConfig()
    {
        // Arrange
        var fixture = new RecurringJobFixture();
        var filter = fixture.GetSut();

        // Act
        filter.OnPerforming(fixture.PerformingContext);

        // Assert
        var monitorConfig = fixture.GetSentMonitorConfig();
        monitorConfig.Should().NotBeNull();
        monitorConfig!.Value.GetProperty("schedule").GetProperty("value").GetString().Should().Be(fixture.Cron);
        monitorConfig.Value.GetProperty("timezone").GetString().Should().Be("UTC");
    }

    [Fact]
    public void OnPerforming_SendMonitorConfigDisabled_SendsCheckInWithoutMonitorConfig()
    {
        // Arrange
        var fixture = new RecurringJobFixture();
        var filter = fixture.GetSut(sendMonitorConfig: false);

        // Act
        filter.OnPerforming(fixture.PerformingContext);

        // Assert
        fixture.AssertCheckInSentWithoutMonitorConfig();
        fixture.Connection.DidNotReceive().GetAllEntriesFromHash(Arg.Any<string>());
    }

    [Fact]
    public void OnPerforming_NotRecurringJob_SendsCheckInWithoutMonitorConfig()
    {
        // Arrange
        var fixture = new RecurringJobFixture { RecurringJobId = null };
        var filter = fixture.GetSut(sendMonitorConfig: true);

        // Act
        filter.OnPerforming(fixture.PerformingContext);

        // Assert
        fixture.AssertCheckInSentWithoutMonitorConfig();
    }

    [Fact]
    public void OnPerforming_RecurringJobDeleted_SendsCheckInWithoutMonitorConfig()
    {
        // Arrange
        var fixture = new RecurringJobFixture { RecurringJobExists = false };
        var filter = fixture.GetSut(sendMonitorConfig: true);

        // Act
        filter.OnPerforming(fixture.PerformingContext);

        // Assert
        fixture.AssertCheckInSentWithoutMonitorConfig();
    }

    [Theory]
    [InlineData("0 0 L * *", "UTC")]
    [InlineData("60 0 * * * *", "UTC")]
    [InlineData("*/10 * * * * *", "UTC")]
    [InlineData("0 * * * *", "Not A Time Zone")]
    public void OnPerforming_ScheduleNotSupportedBySentry_SendsCheckInWithoutMonitorConfig(string cron, string timeZoneId)
    {
        // Arrange
        var fixture = new RecurringJobFixture { Cron = cron, TimeZoneId = timeZoneId };
        var filter = fixture.GetSut(sendMonitorConfig: true);

        // Act
        filter.OnPerforming(fixture.PerformingContext);

        // Assert
        fixture.AssertCheckInSentWithoutMonitorConfig();
        fixture.Logger.DidNotReceive().Log(SentryLevel.Error, Arg.Any<string>(), Arg.Any<Exception?>(), Arg.Any<object[]>());
    }

    [Fact]
    public void OnPerforming_StorageThrows_SendsCheckInWithoutMonitorConfig()
    {
        // Arrange
        var fixture = new RecurringJobFixture();
        var filter = fixture.GetSut(sendMonitorConfig: true);
        fixture.Connection.GetAllEntriesFromHash(Arg.Any<string>()).Throws(new InvalidOperationException());

        // Act
        filter.OnPerforming(fixture.PerformingContext);

        // Assert
        fixture.AssertCheckInSentWithoutMonitorConfig();
    }

    [Fact]
    public void OnPerforming_RecurringJobsShareSlugWithDifferentCron_SendsCheckInWithoutMonitorConfig()
    {
        // Arrange
        var fixture = new RecurringJobFixture { Cron = "0 2 * * *", Job = ReportJob("eu") };
        fixture.OtherRecurringJobs.Add(("report-us", "0 8 * * *", null, ReportJob("us")));
        var filter = fixture.GetSut();

        // Act
        filter.OnPerforming(fixture.PerformingContext);

        // Assert
        fixture.AssertCheckInSentWithoutMonitorConfig();
        fixture.Logger.Received(1).Log(SentryLevel.Debug, Arg.Is<string>(m => m.Contains("with a different schedule")),
            null, Arg.Any<object[]>());
    }

    [Fact]
    public void OnPerforming_RecurringJobsShareSlugWithDifferentTimeZone_SendsCheckInWithoutMonitorConfig()
    {
        // Arrange
        var fixture = new RecurringJobFixture { Cron = "0 2 * * *", Job = ReportJob("eu") };
        fixture.OtherRecurringJobs.Add(("report-us", "0 2 * * *", "America/New_York", ReportJob("us")));
        var filter = fixture.GetSut();

        // Act
        filter.OnPerforming(fixture.PerformingContext);

        // Assert
        fixture.AssertCheckInSentWithoutMonitorConfig();
    }

    [Fact]
    public void OnPerforming_RecurringJobsShareSlugWithSameSchedule_SendsMonitorConfig()
    {
        // Arrange
        var fixture = new RecurringJobFixture { Cron = "0 2 * * *", Job = ReportJob("eu") };
        fixture.OtherRecurringJobs.Add(("report-us", "0 2 * * *", "UTC", ReportJob("us")));
        var filter = fixture.GetSut();

        // Act
        filter.OnPerforming(fixture.PerformingContext);

        // Assert
        fixture.GetSentMonitorConfig().Should().NotBeNull();
    }

    [Fact]
    public void OnPerforming_ClassSlugSharedByMethodsWithDifferentCron_SendsCheckInWithoutMonitorConfig()
    {
        // Arrange
        var fixture = new RecurringJobFixture
        {
            Slug = SlugOnClassJob.Slug,
            Cron = "0 2 * * *",
            Job = Job.FromExpression<SlugOnClassJob>(job => job.First())
        };
        fixture.OtherRecurringJobs.Add(("second", "0 8 * * *", null, Job.FromExpression<SlugOnClassJob>(job => job.Second())));
        var filter = fixture.GetSut();

        // Act
        filter.OnPerforming(fixture.PerformingContext);

        // Assert
        fixture.AssertCheckInSentWithoutMonitorConfig();
    }

    [Fact]
    public void OnPerforming_OtherRecurringJobHasItsOwnSlug_SendsMonitorConfig()
    {
        // Arrange
        var fixture = new RecurringJobFixture { Cron = "0 2 * * *", Job = ReportJob("eu") };
        fixture.OtherRecurringJobs.Add(("other", "0 8 * * *", null, Job.FromExpression<SlugOnClassJob>(job => job.First())));
        var filter = fixture.GetSut();

        // Act
        filter.OnPerforming(fixture.PerformingContext);

        // Assert
        fixture.GetSentMonitorConfig().Should().NotBeNull();
    }

    [Fact]
    public void OnPerforming_WithinTtl_ReadsRecurringJobsOnce()
    {
        // Arrange
        var fixture = new RecurringJobFixture();
        var filter = fixture.GetSut();

        // Act
        filter.OnPerforming(fixture.PerformingContext);
        fixture.Clock.GetUtcNow().Returns(fixture.Now + SentryServerFilter.RecurringJobSlugsTtl - TimeSpan.FromSeconds(1));
        filter.OnPerforming(fixture.PerformingContext);

        // Assert
        fixture.Connection.Received(1).GetAllItemsFromSet("recurring-jobs");
    }

    [Fact]
    public void OnPerforming_AfterTtl_ReadsRecurringJobsAgain()
    {
        // Arrange
        var fixture = new RecurringJobFixture();
        var filter = fixture.GetSut();

        // Act
        filter.OnPerforming(fixture.PerformingContext);
        fixture.Clock.GetUtcNow().Returns(fixture.Now + SentryServerFilter.RecurringJobSlugsTtl);
        filter.OnPerforming(fixture.PerformingContext);

        // Assert
        fixture.Connection.Received(2).GetAllItemsFromSet("recurring-jobs");
    }

    [Fact]
    public void OnPerforming_CronChangedWithinTtl_SendsNewCron()
    {
        // Arrange
        var fixture = new RecurringJobFixture { Cron = "0 2 * * *", Job = ReportJob("eu") };
        var filter = fixture.GetSut();
        filter.OnPerforming(fixture.PerformingContext);
        fixture.Connection.GetAllEntriesFromHash($"recurring-job:{fixture.RecurringJobId}")
            .Returns(new Dictionary<string, string>
            {
                ["Cron"] = "0 8 * * *",
                ["Job"] = InvocationData.SerializeJob(fixture.Job).SerializePayload()
            });
        fixture.Hub.ClearReceivedCalls();

        // Act
        filter.OnPerforming(fixture.PerformingContext);

        // Assert
        var monitorConfig = fixture.GetSentMonitorConfig();
        monitorConfig.Should().NotBeNull();
        monitorConfig!.Value.GetProperty("schedule").GetProperty("value").GetString().Should().Be("0 8 * * *");
    }

    [Fact]
    public void OnPerforming_ReadingAllRecurringJobsThrows_SendsCheckInWithoutMonitorConfig()
    {
        // Arrange
        var fixture = new RecurringJobFixture();
        var filter = fixture.GetSut();
        fixture.Connection.GetAllItemsFromSet("recurring-jobs").Throws(new InvalidOperationException());

        // Act
        filter.OnPerforming(fixture.PerformingContext);

        // Assert
        fixture.AssertCheckInSentWithoutMonitorConfig();
    }

    private static Job ReportJob(string region) => Job.FromExpression<SharedSlugJob>(job => job.Run(region));

    [Theory]
    [InlineData("0 */2 * * *", "0 */2 * * *")]
    [InlineData("30 0 */2 * * *", "0 */2 * * *")]
    [InlineData("0  0   * * 1", "0 0 * * 1")]
    [InlineData("60 0 * * * *", null)]
    [InlineData("*/10 * * * * *", null)]
    [InlineData("0,30 * * * * *", null)]
    [InlineData("0 0 L * *", null)]
    [InlineData("@daily", null)]
    [InlineData("", null)]
    [InlineData("0 9 * * MON", "0 9 * * MON")]
    [InlineData("0 9 1-7 * *", "0 9 1-7 * *")]
    [InlineData("0 9 */2 * MON", "0 9 */2 * MON")]
    [InlineData("0 9 1-7 * */2", "0 9 1-7 * */2")]
    [InlineData("0 9 1-7 * MON", null)]
    [InlineData("0 0 9 15 * 1-5", null)]
    [InlineData("0 9 * * MON-FRI", "0 9 * * MON-FRI")]
    [InlineData("0 9 * * 5-1", null)]
    [InlineData("0 9 * * SAT-SUN", null)]
    [InlineData("0 9 * * mon-sun", null)]
    [InlineData("0 22-2 * * *", null)]
    [InlineData("0 9 * * 1-3,6-0", null)]
    [InlineData("0 8-18/2 * * *", "0 8-18/2 * * *")]
    public void ToCrontab_HangfireCron_ReturnsSentryCrontabOrNull(string cron, string? expected)
    {
        SentryServerFilter.ToCrontab(cron).Should().Be(expected);
    }

    [SkippableTheory]
    [InlineData(null, "UTC")]
    [InlineData("", "UTC")]
    [InlineData("UTC", "UTC")]
    [InlineData("Etc/UTC", "Etc/UTC")]
    [InlineData("Europe/Berlin", "Europe/Berlin")]
    [InlineData("W. Europe Standard Time", "Europe/Berlin")]
    [InlineData("Not A Time Zone", null)]
    [InlineData("Not/A_Time_Zone", null)]
    public void ToIanaTimeZoneId_HangfireTimeZoneId_ReturnsIanaIdOrNull(string? timeZoneId, string? expected)
    {
        Skip.If(expected is not (null or "UTC") && !TestEnvironment.HasTimeZone(timeZoneId!), "No time zone database");

        SentryServerFilter.ToIanaTimeZoneId(timeZoneId).Should().Be(expected);
    }

    public class SharedSlugJob
    {
        [SentryMonitorSlug(RecurringJobFixture.MonitorSlug)]
        public void Run(string region)
        { }
    }

    [SentryMonitorSlug(Slug)]
    public class SlugOnClassJob
    {
        public const string Slug = "slug-on-class";

        public void First()
        { }

        public void Second()
        { }
    }

    private class RecurringJobFixture
    {
        public const string MonitorSlug = "test-monitor-slug";
        private const string JobId = "test-id";

        public string? RecurringJobId { get; set; } = "test-recurring-job";
        public bool RecurringJobExists { get; set; } = true;
        public string Cron { get; set; } = "0 * * * *";
        public string? TimeZoneId { get; set; }
        public Job? Job { get; set; }
        public List<(string Id, string Cron, string? TimeZoneId, Job Job)> OtherRecurringJobs { get; } = new();
        public ISystemClock Clock { get; } = Substitute.For<ISystemClock>();
        public DateTimeOffset Now { get; } = new(2026, 10, 13, 0, 0, 0, TimeSpan.Zero);

        public IStorageConnection Connection { get; } = Substitute.For<IStorageConnection>();
        public IHub Hub { get; } = Substitute.For<IHub>();
        public IDiagnosticLogger Logger { get; } = Substitute.For<IDiagnosticLogger>();
        public SentryId CheckInId { get; set; } = SentryId.Create();
        public PerformingContext PerformingContext { get; private set; } = null!;

        public SentryServerFilter GetSut(bool? sendMonitorConfig = null)
        {
            Logger.IsEnabled(Arg.Any<SentryLevel>()).Returns(true);
            Clock.GetUtcNow().Returns(Now);
            Connection.GetJobParameter(JobId, SentryServerFilter.SentryMonitorSlugKey)
                .Returns(SerializationHelper.Serialize(Slug));
            var recurringJobIds = new HashSet<string>(OtherRecurringJobs.Select(job => job.Id));
            if (RecurringJobId is not null)
            {
                Connection.GetJobParameter(JobId, SentryServerFilter.RecurringJobIdKey)
                    .Returns(SerializationHelper.Serialize(RecurringJobId));
                if (RecurringJobExists)
                {
                    recurringJobIds.Add(RecurringJobId);
                    Connection.GetAllEntriesFromHash($"recurring-job:{RecurringJobId}")
                        .Returns(RecurringJobHash(Cron, TimeZoneId, Job));
                }
            }
            foreach (var (id, cron, timeZoneId, job) in OtherRecurringJobs)
            {
                Connection.GetAllEntriesFromHash($"recurring-job:{id}").Returns(RecurringJobHash(cron, timeZoneId, job));
            }
            Connection.GetAllItemsFromSet("recurring-jobs").Returns(recurringJobIds);

            Hub.CaptureCheckIn(
                    Arg.Any<string>(),
                    Arg.Any<CheckInStatus>(),
                    Arg.Any<SentryId?>(),
                    Arg.Any<TimeSpan?>(),
                    Arg.Any<Scope?>(),
                    Arg.Any<Action<SentryMonitorOptions>?>())
                .Returns(CheckInId);

            var backgroundJob = new BackgroundJob(JobId, null, DateTime.Now);
            var performContext = new PerformContext(null, Connection, backgroundJob, Substitute.For<IJobCancellationToken>());
            PerformingContext = new PerformingContext(performContext);

            var options = new SentryHangfireOptions();
            if (sendMonitorConfig is { } send)
            {
                options.SendMonitorConfig = send;
            }
            return new SentryServerFilter(Hub, Logger, options, Clock);
        }

        public string Slug { get; set; } = MonitorSlug;

        private static Dictionary<string, string> RecurringJobHash(string cron, string? timeZoneId, Job? job)
        {
            var hash = new Dictionary<string, string> { ["Cron"] = cron };
            if (timeZoneId is not null)
            {
                hash["TimeZoneId"] = timeZoneId;
            }
            if (job is not null)
            {
                hash["Job"] = InvocationData.SerializeJob(job).SerializePayload();
            }
            return hash;
        }

        public JsonElement? GetSentMonitorConfig() =>
            Hub.ReceivedConfigureMonitorOptions(Slug) is { } configure ? MonitorConfigJson.Render(configure) : null;

        public void AssertCheckInSentWithoutMonitorConfig()
        {
            Hub.Received(1).CaptureCheckIn(Slug, CheckInStatus.InProgress, Arg.Any<SentryId?>());
            Hub.ReceivedConfigureMonitorOptions(Slug).Should().BeNull();
            PerformingContext.Items[SentryServerFilter.SentryCheckInIdKey].Should().Be(Hub.ReceivedInProgressCheckInId(Slug));
        }
    }
}
