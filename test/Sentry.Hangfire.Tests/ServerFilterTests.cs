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
}
