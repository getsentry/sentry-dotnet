#nullable enable

namespace Sentry.Extensions.AI.Tests;

public class SentryAIOptionsTests
{
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Act
        var options = new SentryAIOptions();

        // Assert
        Assert.True(options.RecordInputs);
        Assert.True(options.RecordOutputs);
    }

    [Fact]
    public void IncludeRequestMessages_CanBeSet()
    {
        // Arrange
        var options = new SentryAIOptions
        {
            // Act
            RecordInputs = false
        };

        // Assert
        Assert.False(options.RecordInputs);
    }

    [Fact]
    public void IncludeResponseContent_CanBeSet()
    {
        // Arrange
        var options = new SentryAIOptions
        {
            // Act
            RecordOutputs = false
        };

        // Assert
        Assert.False(options.RecordOutputs);
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
            RecordInputs = includeRequest,
            RecordOutputs = includeResponse
        };

        // Assert
        Assert.Equal(includeRequest, options.RecordInputs);
        Assert.Equal(includeResponse, options.RecordOutputs);
    }
}
