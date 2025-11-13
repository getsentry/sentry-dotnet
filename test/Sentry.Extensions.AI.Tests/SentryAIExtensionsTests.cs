#nullable enable
using Microsoft.Extensions.AI;

namespace Sentry.Extensions.AI.Tests;

public class SentryAIExtensionsTests
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
    public void WithSentry_IChatClient_ReturnsWrappedClient()
    {
        // Arrange
        var mockClient = Substitute.For<IChatClient>();
        using var listener = SentryAiActivityListener.CreateListener(_fixture.Hub);

        // Act
        var result = mockClient.AddSentry(listener);

        // Assert
        Assert.IsType<SentryChatClient>(result);
    }

    [Fact]
    public void WithSentry_IChatClient_WithConfiguration_PassesConfigurationToWrapper()
    {
        // Arrange
        var mockClient = Substitute.For<IChatClient>();
        using var listener = SentryAiActivityListener.CreateListener(_fixture.Hub);
        var configureWasCalled = false;

        // Act
        var result = mockClient.AddSentry(listener, options =>
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
        using var listener = SentryAiActivityListener.CreateListener(_fixture.Hub);

        // Act
        var result = mockClient.AddSentry(listener, null);

        // Assert
        Assert.IsType<SentryChatClient>(result);
    }
}
