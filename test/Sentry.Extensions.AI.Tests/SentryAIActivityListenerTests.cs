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

    public SentryAIActivityListenerTests()
    {
        // Dispose ActivityListener before each test, otherwise the singleton instance will persist between tests
        SentryAiActivityListener.Dispose();
    }

    [Fact]
    public void Init_AddsActivityListenerToActivitySource()
    {
        // Act
        _ = new SentryAiActivityListener();

        // Assert
        Assert.True(SentryAIActivitySource.Instance.HasListeners());
    }

    [Theory]
    [InlineData(SentryAIConstants.SentryActivitySourceName)]
    public void ShouldListenTo_ReturnsTrueForSentryActivitySource(string sourceName)
    {
        // Arrange
        _ = new SentryAiActivityListener(_fixture.Hub);
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
        _ = new SentryAiActivityListener(_fixture.Hub);
        var activitySource = new ActivitySource("Other.ActivitySource");

        // Act & Assert
        using var activity = activitySource.StartActivity("test");
        Assert.Null(activity); // Activity should not be created for non-Sentry sources

        _fixture.Hub.DidNotReceive().StartTransaction(
            Arg.Any<ITransactionContext>(),
            Arg.Any<IReadOnlyDictionary<string, object?>>());
    }

    [Theory]
    [InlineData("orchestrate_tools")]
    public void Sample_ReturnsAllDataAndRecordedForFICCActivityNames(string activityName)
    {
        // Arrange
        _ = new SentryAiActivityListener(_fixture.Hub);

        // Act
        using var activity = SentryAIActivitySource.Instance.StartActivity(activityName);

        // Assert
        Assert.NotNull(activity);
        Assert.True(activity.IsAllDataRequested);
        Assert.Equal(ActivitySamplingResult.AllDataAndRecorded,
            activity.Recorded ? ActivitySamplingResult.AllDataAndRecorded : ActivitySamplingResult.None);

        _fixture.Hub.Received(1).StartTransaction(
            Arg.Any<ITransactionContext>(),
            Arg.Any<IReadOnlyDictionary<string, object?>>());
    }

    [Fact]
    public void Init_MultipleCalls_NoDuplicateListener_StartsOnlyOneTransaction()
    {
        // Arrange
        _ = new SentryAiActivityListener(_fixture.Hub);
        _ = new SentryAiActivityListener(_fixture.Hub);
        _ = new SentryAiActivityListener(_fixture.Hub);

        // Act
        using var activity = SentryAIActivitySource.Instance.StartActivity(SentryAIConstants.FICCActivityNames[0]);

        // Assert
        Assert.NotNull(activity);
        Assert.True(SentryAIActivitySource.Instance.HasListeners());
        activity.Stop();

        _fixture.Hub.Received(1).StartTransaction(
            Arg.Any<ITransactionContext>(),
            Arg.Any<IReadOnlyDictionary<string, object?>>());
    }
}
