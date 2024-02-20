using Hangfire.Server;

namespace Sentry.Hangfire.Tests;

public class SentryJobFilterTests
{
    private class Fixture
    {
        public IHub Hub { get; set; } = Substitute.For<IHub>();
        public Func<IHub> HubAccessor { get; set; }

        public Fixture()
        {
            Hub.IsEnabled.Returns(true);
            HubAccessor = () => Hub;
        }

        public SentryJobFilter GetSut() => new(HubAccessor);
    }

    private readonly Fixture _fixture = new();

    [Fact]
    public void OnPerforming_MonitorSlugPresentOnContext_CapturesCheckIn()
    {
        var filter = _fixture.GetSut();
        const string monitorSlug = "test-slug";

        filter.OnPerformingInternal(null, (_, _) => monitorSlug, (_, _, _) => { });

        _fixture.Hub.Received(1).CaptureCheckIn(Arg.Is<SentryCheckIn>(checkIn =>
            checkIn.MonitorSlug == monitorSlug &&
            checkIn.Status == CheckInStatus.InProgress));
    }

    [Fact]
    public void OnPerforming_MonitorSlugPresentOnContext_SetsCheckInIdOnContext()
    {
        _fixture.Hub.CaptureCheckIn(Arg.Any<SentryCheckIn>()).Returns(SentryId.Create());
        var filter = _fixture.GetSut();
        var checkInId = SentryId.Empty;

        filter.OnPerformingInternal(null, (_, _) => "test-slug", (_, _, id) => checkInId = id);

        Assert.NotEqual(SentryId.Empty, checkInId);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void OnPerformed_MonitorSlugAndCheckInIdPresentOnContext_CapturesCheckIn(bool hasException)
    {
        var filter = _fixture.GetSut();
        const string monitorSlug = "test-slug";

        object GetJobParameter(PerformContext context, string key)
        {
            return key switch
            {
                SentryJobFilter.SentryMonitorSlugKey => monitorSlug,
                SentryJobFilter.SentryCheckInIdKey => SentryId.Create(),
                _ => null
            };
        }

        bool HasException(PerformedContext context)
        {
            return hasException;
        }

        filter.OnPerformedInternal(null, GetJobParameter, HasException);

        _fixture.Hub.Received(1).CaptureCheckIn(Arg.Is<SentryCheckIn>(checkIn =>
            checkIn.MonitorSlug == monitorSlug &&
            checkIn.Status == (hasException ? CheckInStatus.Error : CheckInStatus.Ok)));
    }
}
