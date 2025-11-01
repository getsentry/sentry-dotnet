#nullable enable
using Sentry.Extensions.AI;

namespace Sentry.Extensions.AI.Tests;

public class SentryAIActivityListenerTests
{
    private class Fixture
    {
        private SentryOptions Options { get; }
        public ISentryClient Client { get; }
        public IHub Hub { get; set; }

        public Fixture()
        {
            Options = new SentryOptions
            {
                Dsn = ValidDsn,
                TracesSampleRate = 1.0,
            };

            Hub = Substitute.For<IHub>();
            Client = Substitute.For<ISentryClient>();
            SentrySdk.Init(Options);
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
        using var activity = SentryAIActivitySource.Instance.StartActivity(activityName);

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
        using var activity = SentryAIActivitySource.Instance.StartActivity(activityName);

        // Assert
        // For non-FICC activity names, the activity may still be created but not recorded
        if (activity != null)
        {
            Assert.False(activity.Recorded);
        }
    }

    [Fact]
    public void Init_MultipleCalls_NoDuplicateListener_StartsOnlyOneTransaction()
    {
        // Arrange
        var sent = 0;
        using var _ = SentrySdk.Init(o =>
        {
            o.Dsn = ValidDsn;
            o.TracesSampleRate = 1.0;
            // Count transactions just before they are sent:
            o.SetBeforeSendTransaction(t =>
            {
                Interlocked.Increment(ref sent);
                return t;
            });
        });

        // Act
        SentryAIActivityListener.Init();
        SentryAIActivityListener.Init();
        SentryAIActivityListener.Init();

        var activity = SentryAIActivitySource.Instance.StartActivity(SentryAIConstants.FICCActivityNames[0]);

        // Assert
        Assert.NotNull(activity);
        Assert.True(SentryAIActivitySource.Instance.HasListeners());
        activity.Stop();

        Assert.Equal(1, Volatile.Read(ref sent));
    }
}
