namespace Sentry.Extensions.AI.Tests;

public class SentryAIOptionsTests
{
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Arrange & Act
        var options = new SentryAIOptions();

        // Assert
        Assert.True(options.Experimental.RecordInputs);
        Assert.True(options.Experimental.RecordOutputs);
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
                RecordInputs = false
            }
        };

        // Assert
        Assert.False(options.Experimental.RecordInputs);
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
                RecordOutputs = false
            }
        };

        // Assert
        Assert.False(options.Experimental.RecordOutputs);
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
    public void ShouldRecord_NotSetAndDataCollectionNotConfigured_RecordsRegardlessOfSendDefaultPii(bool sendDefaultPii)
    {
        var sentryOptions = new SentryOptions { SendDefaultPii = sendDefaultPii };
        var aiOptions = new SentryAIOptions();

        Assert.True(aiOptions.Experimental.ShouldRecordInputs(sentryOptions));
        Assert.True(aiOptions.Experimental.ShouldRecordOutputs(sentryOptions));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void ShouldRecord_SetAndDataCollectionNotConfigured_UsesSetValue(bool recordInputs, bool recordOutputs)
    {
        var sentryOptions = new SentryOptions();
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
    public void ShouldRecord_SetAndDataCollectionConfigured_TakesPrecedenceOverGenAI(bool recordInputs, bool recordOutputs)
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

    [Fact]
    public void ShouldRecord_NotSetAndNoSentryOptions_Records()
    {
        var aiOptions = new SentryAIOptions();

        Assert.True(aiOptions.Experimental.ShouldRecordInputs(null));
        Assert.True(aiOptions.Experimental.ShouldRecordOutputs(null));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ShouldRecordTool_DataCollectionNotConfigured_RecordsRegardlessOfSetValue(bool record)
    {
        var sentryOptions = new SentryOptions();
        var aiOptions = new SentryAIOptions
        {
            Experimental =
            {
                RecordInputs = record,
                RecordOutputs = record
            }
        };

        Assert.True(aiOptions.Experimental.ShouldRecordToolInputs(sentryOptions));
        Assert.True(aiOptions.Experimental.ShouldRecordToolOutputs(sentryOptions));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void ShouldRecordTool_NotSetAndDataCollectionConfigured_FollowsGenAI(bool inputs, bool outputs)
    {
        var sentryOptions = new SentryOptions();
        sentryOptions.ConfigureDataCollection(dataCollection =>
        {
            dataCollection.GenAI.Inputs = inputs;
            dataCollection.GenAI.Outputs = outputs;
        });
        var aiOptions = new SentryAIOptions();

        Assert.Equal(inputs, aiOptions.Experimental.ShouldRecordToolInputs(sentryOptions));
        Assert.Equal(outputs, aiOptions.Experimental.ShouldRecordToolOutputs(sentryOptions));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void ShouldRecordTool_SetAndDataCollectionConfigured_TakesPrecedenceOverGenAI(bool recordInputs,
        bool recordOutputs)
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

        Assert.Equal(recordInputs, aiOptions.Experimental.ShouldRecordToolInputs(sentryOptions));
        Assert.Equal(recordOutputs, aiOptions.Experimental.ShouldRecordToolOutputs(sentryOptions));
    }

    [Fact]
    public void ShouldRecordTool_NoSentryOptions_Records()
    {
        var aiOptions = new SentryAIOptions { Experimental = { RecordInputs = false, RecordOutputs = false } };

        Assert.True(aiOptions.Experimental.ShouldRecordToolInputs(null));
        Assert.True(aiOptions.Experimental.ShouldRecordToolOutputs(null));
    }
}
