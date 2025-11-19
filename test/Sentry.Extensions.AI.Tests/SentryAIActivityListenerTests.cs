#nullable enable

namespace Sentry.Extensions.AI.Tests;

public class SentryAIActivityListenerTests
{
    private class Fixture
    {
        public IHub Hub { get; } = Substitute.For<IHub>();

        public Fixture()
        {
            Hub.IsEnabled.Returns(true);
        }
    }

    private readonly Fixture _fixture = new();

    [Fact]
    public void Init_AddsActivityListenerToActivitySource()
    {
        // Arrange
        var source = SentryAIActivitySource.CreateSource();

        // Act
        using var listener = SentryAIActivityListener.CreateListener(_fixture.Hub);

        // Assert
        Assert.True(source.HasListeners());
    }

    [Fact]
    public void ShouldListenTo_ReturnsTrueForSentryActivitySource()
    {
        // Arrange
        var sourceName = SentryAIActivitySource.SentryActivitySourceName;
        using var listener = SentryAIActivityListener.CreateListener(_fixture.Hub);
        var activitySource = new ActivitySource(sourceName);

        // Act
        using var activity = activitySource.StartActivity(SentryAIConstants.FICCActivityNames[0]);

        // Assert
        Assert.NotNull(activity);
        Assert.NotNull(Activity.Current);

        // Should receive a StartTransaction since we don't already have a transaction going on
        _fixture.Hub.Received(1).StartTransaction(
            Arg.Any<ITransactionContext>(),
            Arg.Any<IReadOnlyDictionary<string, object?>>());
    }

    [Fact]
    public void ShouldListenTo_ReturnsFalseForNonSentryActivitySource()
    {
        // Arrange
        using var listener = SentryAIActivityListener.CreateListener(_fixture.Hub);
        var activitySource = new ActivitySource("Other.ActivitySource");

        // Act & Assert
        using var activity = activitySource.StartActivity("test");
        Assert.Null(activity); // Activity should not be created for non-Sentry sources

        _fixture.Hub.DidNotReceive().StartTransaction(
            Arg.Any<ITransactionContext>(),
            Arg.Any<IReadOnlyDictionary<string, object?>>());
    }

    [Fact]
    public void Sample_ReturnsAllDataAndRecordedForFICCActivityNames()
    {
        // Arrange
        var activityName = "orchestrate_tools";
        using var listener = SentryAIActivityListener.CreateListener(_fixture.Hub);
        var source = SentryAIActivitySource.CreateSource();

        // Act
        using var activity = source.StartActivity(activityName);

        // Assert
        Assert.NotNull(activity);
        Assert.True(activity.IsAllDataRequested);
        Assert.Equal(ActivitySamplingResult.AllDataAndRecorded,
            activity.Recorded ? ActivitySamplingResult.AllDataAndRecorded : ActivitySamplingResult.None);

        _fixture.Hub.Received(1).StartTransaction(
            Arg.Any<ITransactionContext>(),
            Arg.Any<IReadOnlyDictionary<string, object?>>());
    }
}
