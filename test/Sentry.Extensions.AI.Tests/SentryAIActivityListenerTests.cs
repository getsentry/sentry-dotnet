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
        // Act
        SentryAIActivityListener.Init();

        // Assert
        Assert.True(SentryAIActivitySource.Instance.HasListeners());
    }

    [Theory]
    [InlineData(SentryAIConstants.SentryActivitySourceName)]
    public void ShouldListenTo_ReturnsTrueForSentryActivitySource(string sourceName)
    {
        // Arrange
        SentryAIActivityListener.Init(_fixture.Hub);
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
        SentryAIActivityListener.Init(_fixture.Hub);
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
    [InlineData("FunctionInvokingChatClient.GetResponseAsync")]
    [InlineData("FunctionInvokingChatClient")]
    public void Sample_ReturnsAllDataAndRecordedForFICCActivityNames(string activityName)
    {
        // Arrange
        SentryAIActivityListener.Init(_fixture.Hub);

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
        SentryAIActivityListener.Init(_fixture.Hub);
        SentryAIActivityListener.Init(_fixture.Hub);
        SentryAIActivityListener.Init(_fixture.Hub);

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
