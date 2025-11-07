#nullable enable
using Microsoft.Extensions.AI;

namespace Sentry.Extensions.AI.Tests;

public class SentryAIExtensionsTests
{
    [Fact]
    public void WithSentry_IChatClient_ReturnsWrappedClient()
    {
        // Arrange
        var mockClient = Substitute.For<IChatClient>();

        // Act
        var result = mockClient.AddSentry();

        // Assert
        Assert.IsType<SentryChatClient>(result);
    }

    [Fact]
    public void WithSentry_IChatClient_WithConfiguration_PassesConfigurationToWrapper()
    {
        // Arrange
        var mockClient = Substitute.For<IChatClient>();
        var configureWasCalled = false;

        // Act
        var result = mockClient.AddSentry(options =>
            {
                configureWasCalled = true;
                options.Experimental.RecordInputs = false;
                options.Experimental.RecordOutputs = false;
            }
        );

        // Assert
        Assert.IsType<SentryChatClient>(result);
        Assert.True(configureWasCalled);
    }

    [Fact]
    public void WithSentry_IChatClient_WithNullConfiguration_UsesDefaultConfiguration()
    {
        // Arrange
        var mockClient = Substitute.For<IChatClient>();

        // Act
        var result = mockClient.AddSentry(null);

        // Assert
        Assert.IsType<SentryChatClient>(result);
    }
}
