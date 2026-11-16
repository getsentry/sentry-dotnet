namespace Sentry.Extensions.AI.Tests;

public class SentryAIOptionsTests
{
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Arrange & Act
        var options = new SentryAIOptions();

        // Assert
        Assert.False(options.Experimental.RecordInputs);
        Assert.False(options.Experimental.RecordOutputs);
        Assert.Equal("Agent", options.Experimental.AgentName);
    }

    [Fact]
    public void IncludeRequestMessages_CanBeSet()
    {
        // Arrange
        var options = new SentryAIOptions
        {
            // Act
            Experimental =
            {
                RecordInputs = true
            }
        };

        // Assert
        Assert.True(options.Experimental.RecordInputs);
    }

    [Fact]
    public void IncludeResponseContent_CanBeSet()
    {
        // Arrange
        var options = new SentryAIOptions
        {
            // Act
            Experimental =
            {
                RecordOutputs = true
            }
        };

        // Assert
        Assert.True(options.Experimental.RecordOutputs);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void AllPropertyCombinations_WorkCorrectly(bool includeRequest, bool includeResponse)
    {
        // Arrange
        var options = new SentryAIOptions
        {
            // Act
            Experimental =
            {
                RecordInputs = includeRequest,
                RecordOutputs = includeResponse
            }
        };

        // Assert
        Assert.Equal(includeRequest, options.Experimental.RecordInputs);
        Assert.Equal(includeResponse, options.Experimental.RecordOutputs);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ShouldRecord_NotSetAndDataCollectionNotConfigured_FollowsSendDefaultPii(bool sendDefaultPii)
    {
        var sentryOptions = new SentryOptions { SendDefaultPii = sendDefaultPii };
        var aiOptions = new SentryAIOptions();

        Assert.Equal(sendDefaultPii, aiOptions.Experimental.ShouldRecordInputs(sentryOptions));
        Assert.Equal(sendDefaultPii, aiOptions.Experimental.ShouldRecordOutputs(sentryOptions));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void ShouldRecord_NotSetAndDataCollectionConfigured_FollowsGenAI(bool inputs, bool outputs)
    {
        var sentryOptions = new SentryOptions { SendDefaultPii = !inputs };
        sentryOptions.ConfigureDataCollection(dataCollection =>
        {
            dataCollection.GenAI.Inputs = inputs;
            dataCollection.GenAI.Outputs = outputs;
        });
        var aiOptions = new SentryAIOptions();

        Assert.Equal(inputs, aiOptions.Experimental.ShouldRecordInputs(sentryOptions));
        Assert.Equal(outputs, aiOptions.Experimental.ShouldRecordOutputs(sentryOptions));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void ShouldRecord_SetExplicitly_TakesPrecedenceOverDataCollection(bool recordInputs, bool recordOutputs)
    {
        var sentryOptions = new SentryOptions();
        sentryOptions.ConfigureDataCollection(dataCollection =>
        {
            dataCollection.GenAI.Inputs = !recordInputs;
            dataCollection.GenAI.Outputs = !recordOutputs;
        });
        var aiOptions = new SentryAIOptions
        {
            Experimental =
            {
                RecordInputs = recordInputs,
                RecordOutputs = recordOutputs
            }
        };

        Assert.Equal(recordInputs, aiOptions.Experimental.ShouldRecordInputs(sentryOptions));
        Assert.Equal(recordOutputs, aiOptions.Experimental.ShouldRecordOutputs(sentryOptions));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ShouldRecord_SetExplicitly_TakesPrecedenceOverSendDefaultPii(bool record)
    {
        var sentryOptions = new SentryOptions { SendDefaultPii = !record };
        var aiOptions = new SentryAIOptions
        {
            Experimental =
            {
                RecordInputs = record,
                RecordOutputs = record
            }
        };

        Assert.Equal(record, aiOptions.Experimental.ShouldRecordInputs(sentryOptions));
        Assert.Equal(record, aiOptions.Experimental.ShouldRecordOutputs(sentryOptions));
    }

    [Fact]
    public void ShouldRecord_NotSetAndNoSentryOptions_DoesNotRecord()
    {
        var aiOptions = new SentryAIOptions();

        Assert.False(aiOptions.Experimental.ShouldRecordInputs(null));
        Assert.False(aiOptions.Experimental.ShouldRecordOutputs(null));
    }
}
