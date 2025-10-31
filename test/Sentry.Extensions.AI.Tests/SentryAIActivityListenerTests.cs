#nullable enable

namespace Sentry.Extensions.AI.Tests;

public class SentryAIActivityListenerTests
{
    private IHub Hub { get; }
    private ActivitySource SentryActivitySource { get; }
    private ISpan Span { get; }

    public SentryAIActivityListenerTests()
    {
        Hub = Substitute.For<IHub>();
        SentryActivitySource = SentryAIActivitySource.Instance;
        Span = Substitute.For<ISpan>();
        SentrySdk.UseHub(Hub);
    }

    [Fact]
    public void Init_AddsActivityListenerToActivitySource()
    {
        // Act
        SentryAIActivityListener.Init();

        // Assert
        Assert.True(SentryActivitySource.HasListeners());
    }

    [Theory]
    [InlineData(SentryAIConstants.SentryActivitySourceName)]
    public void ShouldListenTo_ReturnsTrueForSentryActivitySource(string sourceName)
    {
        // Arrange
        SentryAIActivityListener.Init();
        var activitySource = new ActivitySource(sourceName);

        // Act & Assert
        using var activity = activitySource.StartActivity(SentryAIConstants.FICCActivityNames[0]);
        Assert.NotNull(activity);
        Assert.NotNull(Activity.Current);
    }

    [Fact]
    public void ShouldListenTo_ReturnsFalseForNonSentryActivitySource()
    {
        // Arrange
        SentryAIActivityListener.Init();
        var activitySource = new ActivitySource("Other.ActivitySource");

        // Act & Assert
        using var activity = activitySource.StartActivity("test");
        Assert.Null(activity); // Activity should not be created for non-Sentry sources
    }

    [Theory]
    [InlineData("orchestrate_tools")]
    [InlineData("FunctionInvokingChatClient.GetResponseAsync")]
    [InlineData("FunctionInvokingChatClient")]
    public void Sample_ReturnsAllDataAndRecordedForFICCActivityNames(string activityName)
    {
        // Arrange
        SentryAIActivityListener.Init();

        // Act
        using var activity = SentryActivitySource.StartActivity(activityName);

        // Assert
        Assert.NotNull(activity);
        Assert.True(activity.IsAllDataRequested);
        Assert.Equal(ActivitySamplingResult.AllDataAndRecorded,
            activity.Recorded ? ActivitySamplingResult.AllDataAndRecorded : ActivitySamplingResult.None);
    }

    [Theory]
    [InlineData("some_other_activity")]
    [InlineData("random_activity_name")]
    public void Sample_ReturnsNoneForNonFICCActivityNames(string activityName)
    {
        // Arrange
        SentryAIActivityListener.Init();

        // Act
        using var activity = SentryActivitySource.StartActivity(activityName);

        // Assert
        // For non-FICC activity names, the activity may still be created but not recorded
        if (activity != null)
        {
            Assert.False(activity.Recorded);
        }
    }
}
