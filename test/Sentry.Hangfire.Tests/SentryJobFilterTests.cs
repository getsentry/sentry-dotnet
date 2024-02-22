using Hangfire.Server;

namespace Sentry.Hangfire.Tests;

public class SentryJobFilterTests
{
    private class Fixture
    {
        public IHub Hub { get; set; } = Substitute.For<IHub>();
        public Func<PerformContext?, string, string?> GetMonitorSlug = (_, _) => string.Empty;
        public Func<PerformContext?, string, SentryId?> GetCheckInId = (_, _) => SentryId.Empty;
        public Action<PerformContext?, string, SentryId> SetCheckInId = (_, _, _) => { };
        public Func<PerformedContext?, bool> HasException = _ => false;

        public Fixture()
        {
            Hub.IsEnabled.Returns(true);
        }

        public SentryJobFilter GetSut() => new(Hub, GetMonitorSlug, GetCheckInId, SetCheckInId, HasException);
    }

    private readonly Fixture _fixture = new();

    [Fact]
    public void OnPerforming_MonitorSlugPresentOnContext_CapturesCheckIn()
    {
        // Arrange
        const string monitorSlug = "test-slug";
        _fixture.GetMonitorSlug = (_, _) => monitorSlug;
        var filter = _fixture.GetSut();

        // Act
        filter.OnPerforming(null);

        // Assert
        _fixture.Hub.Received(1).CaptureCheckIn(Arg.Is<SentryCheckIn>(checkIn =>
            checkIn.MonitorSlug == monitorSlug &&
            checkIn.Status == CheckInStatus.InProgress));
    }

    [Fact]
    public void OnPerforming_MonitorSlugPresentOnContext_SetsCheckInIdOnContext()
    {
        // Arrange
        const string monitorSlug = "test-slug";
        _fixture.GetMonitorSlug = (_, _) => monitorSlug;
        var checkInId = SentryId.Empty;
        _fixture.SetCheckInId = (_, _, id) => checkInId = id;
        _fixture.Hub.CaptureCheckIn(Arg.Any<SentryCheckIn>()).Returns(SentryId.Create());
        var filter = _fixture.GetSut();

        // Act
        filter.OnPerforming(null);

        // Assert
        Assert.NotEqual(SentryId.Empty, checkInId);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void OnPerformed_MonitorSlugAndCheckInIdPresentOnContext_CapturesCheckIn(bool hasException)
    {
        // Arrange
        const string monitorSlug = "test-slug";
        _fixture.GetMonitorSlug = (_, _) => monitorSlug;
        _fixture.GetCheckInId = (_, _) => SentryId.Create();
        _fixture.HasException = _ => hasException;
        var filter = _fixture.GetSut();

        // Act
        filter.OnPerformed(null);

        // Assert
        _fixture.Hub.Received(1).CaptureCheckIn(Arg.Is<SentryCheckIn>(checkIn =>
            checkIn.MonitorSlug == monitorSlug &&
            checkIn.Status == (hasException ? CheckInStatus.Error : CheckInStatus.Ok)));
    }
}
