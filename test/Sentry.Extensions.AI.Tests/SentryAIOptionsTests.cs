namespace Sentry.Extensions.AI.Tests;

public class SentryAIOptionsTests
{
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Act
        var options = new SentryAIOptions();

        // Assert
        Assert.True(options.Experimental.RecordInputs);
        Assert.True(options.Experimental.RecordOutputs);
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
}
