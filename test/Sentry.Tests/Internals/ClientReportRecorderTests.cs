namespace Sentry.Tests.Internals;

public class ClientReportRecorderTests
{
    private readonly Fixture _fixture = new();

    private class Fixture
    {
        public SentryOptions Options { get; } = new();
        public ISystemClock Clock { get; } = new MockClock(DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Ctor_StartsEmpty()
    {
        var sut = new ClientReportRecorder(_fixture.Options, _fixture.Clock);
        Assert.Empty(sut.DiscardedEvents);
    }

    [Fact]
    public void RecordDiscardedEvent_WorksWithDifferentReasonsAndCategories()
    {
        var sut = new ClientReportRecorder(_fixture.Options, _fixture.Clock);

        sut.RecordDiscardedEvent(DiscardReason.QueueOverflow, DataCategory.Transaction);
        sut.RecordDiscardedEvent(DiscardReason.BeforeSend, DataCategory.Attachment);
        sut.RecordDiscardedEvent(DiscardReason.QueueOverflow, DataCategory.Transaction);
        sut.RecordDiscardedEvent(DiscardReason.CacheOverflow, DataCategory.Attachment);
        sut.RecordDiscardedEvent(DiscardReason.QueueOverflow, DataCategory.Transaction);
        sut.RecordDiscardedEvent(DiscardReason.NetworkError, DataCategory.Security);
        sut.RecordDiscardedEvent(DiscardReason.QueueOverflow, DataCategory.Transaction);
        sut.RecordDiscardedEvent(DiscardReason.NetworkError, DataCategory.Session);
        sut.RecordDiscardedEvent(DiscardReason.QueueOverflow, DataCategory.Transaction);
        sut.RecordDiscardedEvent(DiscardReason.EventProcessor, DataCategory.Error);
        sut.RecordDiscardedEvent(DiscardReason.QueueOverflow, DataCategory.Transaction);
        sut.RecordDiscardedEvent(DiscardReason.EventProcessor, DataCategory.Error);
        sut.RecordDiscardedEvent(DiscardReason.QueueOverflow, DataCategory.Transaction);

        sut.DiscardedEvents.Should().BeEquivalentTo(new Dictionary<DiscardReasonWithCategory, int>
        {
            {DiscardReason.BeforeSend.WithCategory(DataCategory.Attachment), 1},
            {DiscardReason.CacheOverflow.WithCategory(DataCategory.Attachment), 1},
            {DiscardReason.NetworkError.WithCategory(DataCategory.Security), 1},
            {DiscardReason.NetworkError.WithCategory(DataCategory.Session), 1},
            {DiscardReason.EventProcessor.WithCategory(DataCategory.Error), 2},
            {DiscardReason.QueueOverflow.WithCategory(DataCategory.Transaction), 7}
        });
    }

    [Fact]
    public void GenerateClientReport_ReturnsNullWhenNothingRecorded()
    {
        var sut = new ClientReportRecorder(_fixture.Options, _fixture.Clock);

        var result = sut.GenerateClientReport();

        Assert.Null(result);
    }

    [Fact]
    public void GenerateClientReport_ReturnsNullWhenClientReportsDisabled()
    {
        _fixture.Options.SendClientReports = false;
        var sut = new ClientReportRecorder(_fixture.Options, _fixture.Clock);
        sut.RecordDiscardedEvent(DiscardReason.EventProcessor, DataCategory.Error);

        var result = sut.GenerateClientReport();

        Assert.Null(result);
    }

    [Fact]
    public void GenerateClientReport_ReturnsClientReport()
    {
        var sut = new ClientReportRecorder(_fixture.Options, _fixture.Clock);
        sut.RecordDiscardedEvent(DiscardReason.EventProcessor, DataCategory.Error);

        var result = sut.GenerateClientReport();

        Assert.NotNull(result);
        Assert.Equal(_fixture.Clock.GetUtcNow(), result.Timestamp);
        result.DiscardedEvents.Should().BeEquivalentTo(new Dictionary<DiscardReasonWithCategory, int>
        {
            {DiscardReason.EventProcessor.WithCategory(DataCategory.Error), 1}
        });
    }

    [Fact]
    public void GenerateClientReport_ResetsCounts()
    {
        var sut = new ClientReportRecorder(_fixture.Options, _fixture.Clock);
        sut.RecordDiscardedEvent(DiscardReason.EventProcessor, DataCategory.Error);
        _ = sut.GenerateClientReport();

        var result = sut.GenerateClientReport();

        Assert.Null(result);
    }

    [Fact]
    public void Load_PopulatesCounters()
    {
        var discardedEvents = new Dictionary<DiscardReasonWithCategory, int>
        {
            {DiscardReason.BeforeSend.WithCategory(DataCategory.Attachment), 1},
            {DiscardReason.EventProcessor.WithCategory(DataCategory.Error), 2},
            {DiscardReason.QueueOverflow.WithCategory(DataCategory.Security), 3}
        };
        var clientReport = new ClientReport(_fixture.Clock.GetUtcNow(), discardedEvents);

        var sut = new ClientReportRecorder(_fixture.Options, _fixture.Clock);
        sut.Load(clientReport);

        sut.DiscardedEvents.Should().BeEquivalentTo(discardedEvents);
    }
}
