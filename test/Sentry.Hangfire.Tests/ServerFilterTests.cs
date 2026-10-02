using System.Text.Json;
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

    [Fact]
    public void OnPerforming_RecurringJobAndSendScheduleEnabled_SendsMonitorConfig()
    {
        // Arrange
        var fixture = new RecurringJobFixture { Cron = "0 */2 * * *", TimeZoneId = "Europe/Berlin" };
        var filter = fixture.GetSut(sendRecurringJobSchedule: true);

        // Act
        filter.OnPerforming(fixture.PerformingContext);

        // Assert
        var monitorConfig = fixture.GetSentMonitorConfig();
        monitorConfig.Should().NotBeNull();
        monitorConfig!.Value.GetProperty("schedule").GetProperty("type").GetString().Should().Be("crontab");
        monitorConfig.Value.GetProperty("schedule").GetProperty("value").GetString().Should().Be("0 */2 * * *");
        monitorConfig.Value.GetProperty("timezone").GetString().Should().Be("Europe/Berlin");
        fixture.PerformingContext.Items[SentryServerFilter.SentryCheckInIdKey].Should().Be(fixture.CheckInId);
    }

    [Fact]
    public void OnPerforming_SendScheduleDisabled_SendsNoMonitorConfig()
    {
        // Arrange
        var fixture = new RecurringJobFixture { Cron = "0 */2 * * *" };
        var filter = fixture.GetSut(sendRecurringJobSchedule: false);

        // Act
        filter.OnPerforming(fixture.PerformingContext);

        // Assert
        fixture.Hub.Received(1).CaptureCheckIn(RecurringJobFixture.MonitorSlug, CheckInStatus.InProgress);
        fixture.ConnectionReceivedNoRecurringJobLookup();
    }

    [Fact]
    public void OnPerforming_NotRecurringJob_SendsNoMonitorConfig()
    {
        // Arrange
        var fixture = new RecurringJobFixture { RecurringJobId = null };
        var filter = fixture.GetSut(sendRecurringJobSchedule: true);

        // Act
        filter.OnPerforming(fixture.PerformingContext);

        // Assert
        fixture.Hub.Received(1).CaptureCheckIn(RecurringJobFixture.MonitorSlug, CheckInStatus.InProgress);
    }

    [Fact]
    public void OnPerforming_ScheduleRejectedBySentry_SendsCheckInWithoutMonitorConfig()
    {
        // Arrange
        var fixture = new RecurringJobFixture { Cron = "0 0 L * *" };
        var filter = fixture.GetSut(sendRecurringJobSchedule: true);

        // Act
        filter.OnPerforming(fixture.PerformingContext);

        // Assert
        fixture.Hub.Received(1).CaptureCheckIn(RecurringJobFixture.MonitorSlug, CheckInStatus.InProgress);
        fixture.PerformingContext.Items[SentryServerFilter.SentryCheckInIdKey].Should().Be(fixture.CheckInId);
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
        fixture.Hub.Received(1).CaptureCheckIn(RecurringJobFixture.MonitorSlug, CheckInStatus.InProgress);
    }

    [Theory]
    [InlineData("0 */2 * * *", "0 */2 * * *")]
    [InlineData("30 0 */2 * * *", "0 */2 * * *")]
    [InlineData("0  0   * * 1", "0 0 * * 1")]
    [InlineData("*/10 * * * * *", null)]
    [InlineData("0,30 * * * * *", null)]
    [InlineData("@daily", null)]
    [InlineData("", null)]
    public void ToCrontab_ReturnsFiveFieldCrontab(string cron, string? expected)
    {
        SentryServerFilter.ToCrontab(cron).Should().Be(expected);
    }

    [Theory]
    [InlineData(null, "UTC")]
    [InlineData("", "UTC")]
    [InlineData("Europe/Berlin", "Europe/Berlin")]
    [InlineData("Not A Time Zone", null)]
    public void ToIanaTimeZoneId_ReturnsIanaId(string? timeZoneId, string? expected)
    {
        SentryServerFilter.ToIanaTimeZoneId(timeZoneId).Should().Be(expected);
    }

    [Fact]
    public void ToIanaTimeZoneId_WindowsId_ReturnsIanaId()
    {
        SentryServerFilter.ToIanaTimeZoneId("W. Europe Standard Time").Should().Be("Europe/Berlin");
    }

    private class RecurringJobFixture
    {
        public const string MonitorSlug = "test-monitor-slug";
        private const string JobId = "test-id";

        public string? RecurringJobId { get; set; } = "test-recurring-job";
        public string Cron { get; set; } = "0 * * * *";
        public string? TimeZoneId { get; set; }

        public IStorageConnection Connection { get; } = Substitute.For<IStorageConnection>();
        public IHub Hub { get; } = Substitute.For<IHub>();
        public SentryId CheckInId { get; } = SentryId.Create();
        public PerformingContext PerformingContext { get; private set; } = null!;

        private Action<SentryMonitorOptions>? _configureMonitorOptions;

        public SentryServerFilter GetSut(bool sendRecurringJobSchedule)
        {
            Connection.GetJobParameter(JobId, SentryServerFilter.SentryMonitorSlugKey)
                .Returns(SerializationHelper.Serialize(MonitorSlug));
            if (RecurringJobId is not null)
            {
                Connection.GetJobParameter(JobId, SentryServerFilter.RecurringJobIdKey)
                    .Returns(SerializationHelper.Serialize(RecurringJobId));
                var recurringJob = new Dictionary<string, string> { ["Cron"] = Cron };
                if (TimeZoneId is not null)
                {
                    recurringJob["TimeZoneId"] = TimeZoneId;
                }
                Connection.GetAllEntriesFromHash($"recurring-job:{RecurringJobId}").Returns(recurringJob);
            }

            // Mirrors Hub: a throwing callback means the check-in is not sent
            Hub.CaptureCheckIn(
                    Arg.Any<string>(),
                    Arg.Any<CheckInStatus>(),
                    Arg.Any<SentryId?>(),
                    Arg.Any<TimeSpan?>(),
                    Arg.Any<Scope?>(),
                    Arg.Any<Action<SentryMonitorOptions>?>())
                .Returns(callInfo =>
                {
                    var configure = callInfo.ArgAt<Action<SentryMonitorOptions>?>(5);
                    try
                    {
                        configure?.Invoke(new SentryMonitorOptions());
                    }
                    catch
                    {
                        return SentryId.Empty;
                    }
                    _configureMonitorOptions = configure;
                    return CheckInId;
                });

            var backgroundJob = new BackgroundJob(JobId, null, DateTime.Now);
            var performContext = new PerformContext(null, Connection, backgroundJob, Substitute.For<IJobCancellationToken>());
            PerformingContext = new PerformingContext(performContext);

            return new SentryServerFilter(Hub, Substitute.For<IDiagnosticLogger>(),
                new SentryHangfireOptions { SendRecurringJobSchedule = sendRecurringJobSchedule });
        }

        public JsonElement? GetSentMonitorConfig()
        {
            if (_configureMonitorOptions is null)
            {
                return null;
            }

            var options = new SentryMonitorOptions();
            _configureMonitorOptions(options);
            var checkIn = new SentryCheckIn(MonitorSlug, CheckInStatus.InProgress) { MonitorOptions = options };

            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                checkIn.WriteTo(writer, null);
            }

            using var document = JsonDocument.Parse(stream.ToArray());
            return document.RootElement.GetProperty("monitor_config").Clone();
        }

        public void ConnectionReceivedNoRecurringJobLookup() =>
            Connection.DidNotReceive().GetAllEntriesFromHash(Arg.Any<string>());
    }
}
