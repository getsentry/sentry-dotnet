using Microsoft.Extensions.AI;

namespace Sentry.Extensions.AI.Tests;

public class SentryInstrumentedFunctionTests
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
    public async Task InvokeCoreAsync_WithValidFunction_ReturnsResult()
    {
        // Arrange
        var testFunction = AIFunctionFactory.Create(() => "test result", "TestFunction", "Test function description");
        var sentryFunction = new SentryInstrumentedFunction(testFunction, _fixture.Hub);
        var arguments = new AIFunctionArguments();

        // Act
        var result = await sentryFunction.InvokeAsync(arguments);

        // Assert
        // AIFunctionFactory returns JsonElement, so we need to check the actual content
        Assert.NotNull(result);
        var jsonElement = Assert.IsType<JsonElement>(result);
        Assert.Equal("test result", jsonElement.GetString());

        Assert.Equal("TestFunction", sentryFunction.Name);
        Assert.Equal("Test function description", sentryFunction.Description);
        _fixture.Hub.Received(1).StartTransaction(
            Arg.Any<ITransactionContext>(),
            Arg.Any<IReadOnlyDictionary<string, object?>>());
    }

    [Fact]
    public async Task InvokeCoreAsync_WithNullResult_ReturnsNull()
    {
        // Arrange
        var testFunction = AIFunctionFactory.Create(object? () => null, "TestFunction", "Test function description");
        var sentryFunction = new SentryInstrumentedFunction(testFunction, _fixture.Hub);
        var arguments = new AIFunctionArguments();

        // Act
        var result = await sentryFunction.InvokeAsync(arguments);

        // Assert
        Assert.NotNull(result);
        var jsonElement = Assert.IsType<JsonElement>(result);
        Assert.Equal(JsonValueKind.Null, jsonElement.ValueKind);

        _fixture.Hub.Received(1).StartTransaction(
            Arg.Any<ITransactionContext>(),
            Arg.Any<IReadOnlyDictionary<string, object?>>());
    }

    [Fact]
    public async Task InvokeCoreAsync_WithJsonNullResult_ReturnsJsonElement()
    {
        // Arrange
        var jsonNullElement = JsonSerializer.Deserialize<JsonElement>("null");
        var testFunction = AIFunctionFactory.Create(() => jsonNullElement, "TestFunction", "Test function description");
        var sentryFunction = new SentryInstrumentedFunction(testFunction, _fixture.Hub);
        var arguments = new AIFunctionArguments();

        // Act
        var result = await sentryFunction.InvokeAsync(arguments);

        // Assert
        Assert.NotNull(result);
        var jsonResult = Assert.IsType<JsonElement>(result);
        Assert.Equal(JsonValueKind.Null, jsonResult.ValueKind);

        _fixture.Hub.Received(1).StartTransaction(
            Arg.Any<ITransactionContext>(),
            Arg.Any<IReadOnlyDictionary<string, object?>>());
    }

    [Fact]
    public async Task InvokeCoreAsync_WithJsonElementResult_CallsToStringForSpanOutput()
    {
        // Arrange
        var jsonElement = JsonSerializer.Deserialize<JsonElement>("\"test output\"");
        var testFunction = AIFunctionFactory.Create(() => jsonElement, "TestFunction", "Test function description");
        var sentryFunction = new SentryInstrumentedFunction(testFunction, _fixture.Hub);
        var arguments = new AIFunctionArguments();

        // Act
        var result = await sentryFunction.InvokeAsync(arguments);

        // Assert
        Assert.NotNull(result);
        var jsonResult = Assert.IsType<JsonElement>(result);
        Assert.Equal("test output", jsonResult.GetString());

        _fixture.Hub.Received(1).StartTransaction(
            Arg.Any<ITransactionContext>(),
            Arg.Any<IReadOnlyDictionary<string, object?>>());
    }

    [Fact]
    public async Task InvokeCoreAsync_WithComplexResult_ReturnsObject()
    {
        // Arrange
        var resultObject = new
        {
            message = "test",
            count = 42
        };
        var testFunction = AIFunctionFactory.Create(() => resultObject, "TestFunction", "Test function description");
        var sentryFunction = new SentryInstrumentedFunction(testFunction, _fixture.Hub);
        var arguments = new AIFunctionArguments();

        // Act
        var result = await sentryFunction.InvokeAsync(arguments);

        // Assert
        Assert.NotNull(result);
        var jsonElement = Assert.IsType<JsonElement>(result);
        var message = jsonElement.GetProperty("message").GetString();
        var count = jsonElement.GetProperty("count").GetInt32();
        Assert.Equal("test", message);
        Assert.Equal(42, count);

        _fixture.Hub.Received(1).StartTransaction(
            Arg.Any<ITransactionContext>(),
            Arg.Any<IReadOnlyDictionary<string, object?>>());
    }

    [Fact]
    public async Task InvokeCoreAsync_WhenFunctionThrows_PropagatesException()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Test exception");
        var testFunction = AIFunctionFactory.Create(new Func<object>(() => throw expectedException), "TestFunction",
            "Test function description");
        var sentryFunction = new SentryInstrumentedFunction(testFunction, _fixture.Hub);
        var arguments = new AIFunctionArguments();

        // Act & Assert
        var actualException = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await sentryFunction.InvokeAsync(arguments));

        Assert.Equal(expectedException.Message, actualException.Message);
        _fixture.Hub.Received(1).StartTransaction(
            Arg.Any<ITransactionContext>(),
            Arg.Any<IReadOnlyDictionary<string, object?>>());
    }

    [Fact]
    public async Task InvokeCoreAsync_WithCancellation_PropagatesCancellation()
    {
        // Arrange
        var testFunction = AIFunctionFactory.Create((CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return "result";
        }, "TestFunction", "Test function description");

        var sentryFunction = new SentryInstrumentedFunction(testFunction, _fixture.Hub);
        var arguments = new AIFunctionArguments();
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await sentryFunction.InvokeAsync(arguments, cts.Token));

        _fixture.Hub.Received(1).StartTransaction(
            Arg.Any<ITransactionContext>(),
            Arg.Any<IReadOnlyDictionary<string, object?>>());
    }

    [Fact]
    public async Task InvokeCoreAsync_WithParameters_PassesParametersCorrectly()
    {
        // Arrange
        var receivedArguments = (AIFunctionArguments?)null;
        var testFunction = AIFunctionFactory.Create((AIFunctionArguments args) =>
        {
            receivedArguments = args;
            return "result";
        }, "TestFunction", "Test function description");

        var sentryFunction = new SentryInstrumentedFunction(testFunction, _fixture.Hub);
        var arguments = new AIFunctionArguments
        {
            ["param1"] = "value1"
        };

        // Act
        await sentryFunction.InvokeAsync(arguments);

        // Assert
        Assert.NotNull(receivedArguments);
        Assert.Equal("value1", receivedArguments["param1"]);

        _fixture.Hub.Received(1).StartTransaction(
            Arg.Any<ITransactionContext>(),
            Arg.Any<IReadOnlyDictionary<string, object?>>());
    }

    [Fact]
    public void Constructor_PreservesInnerFunctionProperties()
    {
        // Arrange
        var testFunction = AIFunctionFactory.Create(() => "test", "TestFunction", "Test function description");

        // Act
        var sentryFunction = new SentryInstrumentedFunction(testFunction, _fixture.Hub);

        // Assert
        Assert.Equal("TestFunction", sentryFunction.Name);
        Assert.Equal("Test function description", sentryFunction.Description);
    }
}
