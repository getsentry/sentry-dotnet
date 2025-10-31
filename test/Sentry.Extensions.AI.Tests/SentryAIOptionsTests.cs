#nullable enable
using Sentry.Extensions.AI;

namespace Sentry.Extensions.AI.Tests;

public class SentryAIOptionsTests
{
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Act
        var options = new SentryAIOptions();

        // Assert
        Assert.True(options.IncludeAIRequestMessages);
        Assert.True(options.IncludeAIResponseContent);
        Assert.False(options.InitializeSdk);
    }

    [Fact]
    public void IncludeRequestMessages_CanBeSet()
    {
        // Arrange
        var options = new SentryAIOptions
        {
            // Act
            IncludeAIRequestMessages = false
        };

        // Assert
        Assert.False(options.IncludeAIRequestMessages);
    }

    [Fact]
    public void IncludeResponseContent_CanBeSet()
    {
        // Arrange
        var options = new SentryAIOptions
        {
            // Act
            IncludeAIResponseContent = false
        };

        // Assert
        Assert.False(options.IncludeAIResponseContent);
    }

    [Fact]
    public void InitializeSdk_CanBeSet()
    {
        // Arrange
        var options = new SentryAIOptions();

        // Act
        options.InitializeSdk = true;

        // Assert
        Assert.True(options.InitializeSdk);
    }

    [Fact]
    public void InheritsFromSentryOptions()
    {
        // Arrange & Act
        var options = new SentryAIOptions();

        // Assert
        Assert.IsType<SentryOptions>(options, exactMatch: false);
    }

    [Fact]
    public void CanSetSentryOptionsProperties()
    {
        // Arrange
        var options = new SentryAIOptions();

        // Act
        options.Dsn = "https://key@sentry.io/project";
        options.Environment = "test";
        options.Release = "1.0.0";

        // Assert
        Assert.Equal("https://key@sentry.io/project", options.Dsn);
        Assert.Equal("test", options.Environment);
        Assert.Equal("1.0.0", options.Release);
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, true, false)]
    [InlineData(true, false, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, true)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    [InlineData(false, false, false)]
    public void AllPropertyCombinations_WorkCorrectly(bool includeRequest, bool includeResponse, bool initializeSdk)
    {
        // Arrange
        var options = new SentryAIOptions
        {
            // Act
            IncludeAIRequestMessages = includeRequest,
            IncludeAIResponseContent = includeResponse,
            InitializeSdk = initializeSdk
        };

        // Assert
        Assert.Equal(includeRequest, options.IncludeAIRequestMessages);
        Assert.Equal(includeResponse, options.IncludeAIResponseContent);
        Assert.Equal(initializeSdk, options.InitializeSdk);
    }
}
