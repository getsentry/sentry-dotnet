using Hangfire;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.Storage;

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
        hub.CaptureCheckIn(monitorSlug, CheckInStatus.InProgress).Returns(SentryId.Create());

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
        performingContext.Items[SentryServerFilter.SentryCheckInIdKey].Should().NotBeSameAs(firstKey);
    }

    [SkippableTheory]
    [InlineData("0 */2 * * *", "Europe/Berlin", "0 */2 * * *", "Europe/Berlin")]
    [InlineData("30 0 */2 * * *", "UTC", "0 */2 * * *", "UTC")]
    [InlineData("0 0 * * MON", null, "0 0 * * MON", "UTC")]
    public void OnPerforming_RecurringJobWithSendScheduleEnabled_SendsMonitorConfig(
        string cron, string? timeZoneId, string expectedCrontab, string expectedTimeZone)
    {
        Skip.If(timeZoneId is not (null or "UTC") && !TestEnvironment.HasTimeZone(timeZoneId), "No time zone database");

        // Arrange
        var fixture = new RecurringJobFixture { Cron = cron, TimeZoneId = timeZoneId };
        var filter = fixture.GetSut(sendRecurringJobSchedule: true);

        // Act
        filter.OnPerforming(fixture.PerformingContext);

        // Assert
        var monitorConfig = fixture.GetSentMonitorConfig();
        monitorConfig.Should().NotBeNull();
        monitorConfig!.Value.GetProperty("schedule").GetProperty("type").GetString().Should().Be("crontab");
        monitorConfig.Value.GetProperty("schedule").GetProperty("value").GetString().Should().Be(expectedCrontab);
        monitorConfig.Value.GetProperty("timezone").GetString().Should().Be(expectedTimeZone);
        fixture.PerformingContext.Items[SentryServerFilter.SentryCheckInIdKey].Should().Be(fixture.CheckInId);
    }

    [Fact]
    public void OnPerforming_SendScheduleDisabled_SendsCheckInWithoutMonitorConfig()
    {
        // Arrange
        var fixture = new RecurringJobFixture();
        var filter = fixture.GetSut(sendRecurringJobSchedule: false);

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
        var filter = fixture.GetSut(sendRecurringJobSchedule: true);

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
        var filter = fixture.GetSut(sendRecurringJobSchedule: true);

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
        var filter = fixture.GetSut(sendRecurringJobSchedule: true);

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
        var filter = fixture.GetSut(sendRecurringJobSchedule: true);
        fixture.Connection.GetAllEntriesFromHash(Arg.Any<string>()).Throws(new InvalidOperationException());

        // Act
        filter.OnPerforming(fixture.PerformingContext);

        // Assert
        fixture.AssertCheckInSentWithoutMonitorConfig();
    }

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

    private class RecurringJobFixture
    {
        public const string MonitorSlug = "test-monitor-slug";
        private const string JobId = "test-id";

        public string? RecurringJobId { get; set; } = "test-recurring-job";
        public bool RecurringJobExists { get; set; } = true;
        public string Cron { get; set; } = "0 * * * *";
        public string? TimeZoneId { get; set; }

        public IStorageConnection Connection { get; } = Substitute.For<IStorageConnection>();
        public IHub Hub { get; } = Substitute.For<IHub>();
        public IDiagnosticLogger Logger { get; } = Substitute.For<IDiagnosticLogger>();
        public SentryId CheckInId { get; } = SentryId.Create();
        public PerformingContext PerformingContext { get; private set; } = null!;

        public SentryServerFilter GetSut(bool sendRecurringJobSchedule)
        {
            Logger.IsEnabled(Arg.Any<SentryLevel>()).Returns(true);
            Connection.GetJobParameter(JobId, SentryServerFilter.SentryMonitorSlugKey)
                .Returns(SerializationHelper.Serialize(MonitorSlug));
            if (RecurringJobId is not null)
            {
                Connection.GetJobParameter(JobId, SentryServerFilter.RecurringJobIdKey)
                    .Returns(SerializationHelper.Serialize(RecurringJobId));
                if (RecurringJobExists)
                {
                    var recurringJob = new Dictionary<string, string> { ["Cron"] = Cron };
                    if (TimeZoneId is not null)
                    {
                        recurringJob["TimeZoneId"] = TimeZoneId;
                    }
                    Connection.GetAllEntriesFromHash($"recurring-job:{RecurringJobId}").Returns(recurringJob);
                }
            }

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

            return new SentryServerFilter(Hub, Logger,
                new SentryHangfireOptions { SendRecurringJobSchedule = sendRecurringJobSchedule });
        }

        public JsonElement? GetSentMonitorConfig() =>
            Hub.ReceivedConfigureMonitorOptions(MonitorSlug) is { } configure ? MonitorConfigJson.Render(configure) : null;

        public void AssertCheckInSentWithoutMonitorConfig()
        {
            Hub.Received(1).CaptureCheckIn(MonitorSlug, CheckInStatus.InProgress);
            Hub.ReceivedConfigureMonitorOptions(MonitorSlug).Should().BeNull();
            PerformingContext.Items[SentryServerFilter.SentryCheckInIdKey].Should().Be(CheckInId);
        }
    }
}
