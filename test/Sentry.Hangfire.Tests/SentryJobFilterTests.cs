using Hangfire.Server;

namespace Sentry.Hangfire.Tests;

public class SentryJobFilterTests
{
    private class Fixture
    {
        public IHub Hub { get; set; } = Substitute.For<IHub>();

        public Fixture()
        {
            Hub.IsEnabled.Returns(true);
        }

        public SentryJobFilter GetSut() => new(Hub);
    }

    private readonly Fixture _fixture = new();

    [Fact]
    public void OnPerforming_MonitorSlugPresentOnContext_CapturesCheckIn()
    {
        // Arrange
        const string monitorSlug = "test-slug";
        var items = new Dictionary<string, object> { { SentryJobFilter.SentryMonitorSlugKey, monitorSlug } };
        var filter = _fixture.GetSut();

        // Act
        filter.OnPerformingInternal(items);

        // Assert
        _fixture.Hub.Received(1).CaptureCheckIn(Arg.Is<SentryCheckIn>(checkIn =>
            checkIn.MonitorSlug == monitorSlug &&
            checkIn.Status == CheckInStatus.InProgress));
    }

    [Fact]
    public void OnPerforming_MonitorSlugPresentOnContext_SetsCheckInIdInItems()
    {
        // Arrange
        const string monitorSlug = "test-slug";
        var items = new Dictionary<string, object> { { SentryJobFilter.SentryMonitorSlugKey, monitorSlug } };
        _fixture.Hub.CaptureCheckIn(Arg.Any<SentryCheckIn>()).Returns(SentryId.Create());
        var filter = _fixture.GetSut();

        // Act
        filter.OnPerformingInternal(items);

        // Assert
        items.TryGetValue(SentryJobFilter.SentryCheckInIdKey, out var checkInIdObject);
        Assert.IsType<SentryId>(checkInIdObject);
        Assert.NotEqual(SentryId.Empty, (SentryId)checkInIdObject);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void OnPerformed_MonitorSlugAndCheckInIdPresentOnContext_CapturesCheckIn(bool hasException)
    {
        // Arrange
        const string monitorSlug = "test-slug";
        var items = new Dictionary<string, object>
        {
            { SentryJobFilter.SentryMonitorSlugKey, monitorSlug },
            {SentryJobFilter.SentryCheckInIdKey, SentryId.Create()}
        };
        var filter = _fixture.GetSut();

        // Act
        filter.OnPerformedInternal(items, hasException);

        // Assert
        _fixture.Hub.Received(1).CaptureCheckIn(Arg.Is<SentryCheckIn>(checkIn =>
            checkIn.MonitorSlug == monitorSlug &&
            checkIn.Status == (hasException ? CheckInStatus.Error : CheckInStatus.Ok)));
    }
}
