#nullable enable
using Microsoft.Extensions.AI;
using NSubstitute;
using Sentry.Extensions.AI;

namespace Sentry.Extensions.AI.Tests;

public class SentryAIExtensionsTests
{
    [Fact]
    public void WithSentry_ChatOptions_WithNullTools_ReturnsOriginalOptions()
    {
        // Arrange
        var options = new ChatOptions();

        // Act
        var result = options.WithSentry();

        // Assert
        Assert.Same(options, result);
    }

    [Fact]
    public void WithSentry_ChatOptions_WithEmptyTools_ReturnsOriginalOptions()
    {
        // Arrange
        var options = new ChatOptions
        {
            Tools = new List<AITool>()
        };

        // Act
        var result = options.WithSentry();

        // Assert
        Assert.Same(options, result);
    }

    [Fact]
    public void WithSentry_ChatOptions_WrapsAIFunctionsWithSentryInstrumentedFunction()
    {
        // Arrange
        var mockFunction = Substitute.For<AIFunction>();
        mockFunction.Name.Returns("TestFunction");
        mockFunction.Description.Returns("Test Description");

        var options = new ChatOptions
        {
            Tools = new List<AITool> { mockFunction }
        };

        // Act
        var result = options.WithSentry();

        // Assert
        Assert.Same(options, result);
        Assert.Single(options.Tools);
        Assert.IsType<SentryInstrumentedFunction>(options.Tools[0]);

        var instrumentedFunction = (SentryInstrumentedFunction)options.Tools[0];
        Assert.Equal("TestFunction", instrumentedFunction.Name);
        Assert.Equal("Test Description", instrumentedFunction.Description);
    }

    [Fact]
    public void WithSentry_ChatOptions_DoesNotDoubleWrapSentryInstrumentedFunction()
    {
        // Arrange
        var mockFunction = Substitute.For<AIFunction>();
        var alreadyInstrumentedFunction = new SentryInstrumentedFunction(mockFunction);

        var options = new ChatOptions
        {
            Tools = new List<AITool> { alreadyInstrumentedFunction }
        };

        // Act
        var result = options.WithSentry();

        // Assert
        Assert.Same(options, result);
        Assert.Single(options.Tools);
        Assert.Same(alreadyInstrumentedFunction, options.Tools[0]);
    }

    [Fact]
    public void WithSentry_ChatOptions_HandlesMultipleFunctions()
    {
        // Arrange
        var mockFunction1 = Substitute.For<AIFunction>();
        mockFunction1.Name.Returns("Function1");

        var mockFunction2 = Substitute.For<AIFunction>();
        mockFunction2.Name.Returns("Function2");

        var alreadyInstrumentedFunction = new SentryInstrumentedFunction(mockFunction1);

        var options = new ChatOptions
        {
            Tools = new List<AITool>
            {
                mockFunction1,
                mockFunction2,
                alreadyInstrumentedFunction
            }
        };

        // Act
        var result = options.WithSentry();

        // Assert
        Assert.Same(options, result);
        Assert.Equal(3, options.Tools.Count);

        // First function should be wrapped
        Assert.IsType<SentryInstrumentedFunction>(options.Tools[0]);
        Assert.Equal("Function1", options.Tools[0].Name);

        // Second function should be wrapped
        Assert.IsType<SentryInstrumentedFunction>(options.Tools[1]);
        Assert.Equal("Function2", options.Tools[1].Name);

        // Third function was already instrumented, should remain unchanged
        Assert.Same(alreadyInstrumentedFunction, options.Tools[2]);
    }

    [Fact]
    public void WithSentry_ChatOptions_IgnoresNonAIFunctionTools()
    {
        // Arrange
        var mockFunction = Substitute.For<AIFunction>();
        mockFunction.Name.Returns("TestFunction");

        var mockNonFunction = Substitute.For<AITool>();
        mockNonFunction.Name.Returns("NonFunction");

        var options = new ChatOptions
        {
            Tools = new List<AITool> { mockFunction, mockNonFunction }
        };

        // Act
        var result = options.WithSentry();

        // Assert
        Assert.Same(options, result);
        Assert.Equal(2, options.Tools.Count);

        // AIFunction should be wrapped
        Assert.IsType<SentryInstrumentedFunction>(options.Tools[0]);
        Assert.Equal("TestFunction", options.Tools[0].Name);

        // Non-AIFunction should remain unchanged
        Assert.Same(mockNonFunction, options.Tools[1]);
    }

    [Fact]
    public void WithSentry_IChatClient_ReturnsWrappedClient()
    {
        // Arrange
        var mockClient = Substitute.For<IChatClient>();

        // Act
        var result = mockClient.WithSentry();

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
        var result = mockClient.WithSentry(Configure);

        // Assert
        Assert.IsType<SentryChatClient>(result);
        Assert.True(configureWasCalled);
        return;

        void Configure(SentryAIOptions options)
        {
            configureWasCalled = true;
            options.IncludeAIRequestMessages = false;
            options.IncludeAIResponseContent = false;
        }
    }

    [Fact]
    public void WithSentry_IChatClient_WithNullConfiguration_UsesDefaultConfiguration()
    {
        // Arrange
        var mockClient = Substitute.For<IChatClient>();

        // Act
        var result = mockClient.WithSentry(null);

        // Assert
        Assert.IsType<SentryChatClient>(result);
    }
}
