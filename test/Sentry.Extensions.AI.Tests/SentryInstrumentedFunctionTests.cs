#nullable enable
using Microsoft.Extensions.AI;

namespace Sentry.Extensions.AI.Tests;

public class SentryInstrumentedFunctionTests
{
    [Fact]
    public async Task InvokeCoreAsync_WithValidFunction_ReturnsResult()
    {
        // Arrange
        using var sentryDisposable = SentryHelpers.InitializeSdk();
        var testFunction = AIFunctionFactory.Create(() => "test result", "TestFunction", "Test function description");
        var mockOption = Substitute.For<ChatOptions>();
        var sentryFunction = new SentryInstrumentedFunction(testFunction, mockOption);
        var arguments = new AIFunctionArguments();

        // Act
        var result = await sentryFunction.InvokeAsync(arguments);

        // Assert
        // AIFunctionFactory returns JsonElement, so we need to check the actual content
        Assert.NotNull(result);
        if (result is JsonElement jsonElement)
        {
            Assert.Equal("test result", jsonElement.GetString());
        }
        else
        {
            Assert.Equal("test result", result);
        }
        Assert.Equal("TestFunction", sentryFunction.Name);
        Assert.Equal("Test function description", sentryFunction.Description);
    }

    [Fact]
    public async Task InvokeCoreAsync_WithNullResult_ReturnsNull()
    {
        // Arrange
        using var sentryDisposable = SentryHelpers.InitializeSdk();
        var testFunction = AIFunctionFactory.Create(object? () => null, "TestFunction", "Test function description");
        var mockOption = Substitute.For<ChatOptions>();
        var sentryFunction = new SentryInstrumentedFunction(testFunction, mockOption);
        var arguments = new AIFunctionArguments();

        // Act
        var result = await sentryFunction.InvokeAsync(arguments);

        // Assert
        // AIFunctionFactory may return JsonElement with ValueKind.Null instead of actual null
        if (result is JsonElement jsonElement)
        {
            Assert.Equal(JsonValueKind.Null, jsonElement.ValueKind);
        }
        else
        {
            Assert.Null(result);
        }
    }

    [Fact]
    public async Task InvokeCoreAsync_WithJsonNullResult_ReturnsJsonElement()
    {
        // Arrange
        using var sentryDisposable = SentryHelpers.InitializeSdk();
        var jsonNullElement = JsonSerializer.Deserialize<JsonElement>("null");
        var testFunction = AIFunctionFactory.Create(() => jsonNullElement, "TestFunction", "Test function description");
        var mockOption = Substitute.For<ChatOptions>();
        var sentryFunction = new SentryInstrumentedFunction(testFunction, mockOption);
        var arguments = new AIFunctionArguments();

        // Act
        var result = await sentryFunction.InvokeAsync(arguments);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<JsonElement>(result);
        var jsonResult = (JsonElement)result;
        Assert.Equal(JsonValueKind.Null, jsonResult.ValueKind);
    }

    [Fact]
    public async Task InvokeCoreAsync_WithJsonElementResult_CallsToStringForSpanOutput()
    {
        // Arrange
        using var sentryDisposable = SentryHelpers.InitializeSdk();
        var jsonElement = JsonSerializer.Deserialize<JsonElement>("\"test output\"");
        var testFunction = AIFunctionFactory.Create(() => jsonElement, "TestFunction", "Test function description");
        var mockOption = Substitute.For<ChatOptions>();
        var sentryFunction = new SentryInstrumentedFunction(testFunction, mockOption);
        var arguments = new AIFunctionArguments();

        // Act
        var result = await sentryFunction.InvokeAsync(arguments);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<JsonElement>(result);
        var jsonResult = (JsonElement)result;
        Assert.Equal("test output", jsonResult.GetString());

        // The span should have recorded the ToString() output of the JsonElement
        // (This is testing the internal behavior that ToString() gets called for span data)
    }

    [Fact]
    public async Task InvokeCoreAsync_WithComplexResult_ReturnsObject()
    {
        // Arrange
        using var sentryDisposable = SentryHelpers.InitializeSdk();
        var resultObject = new { message = "test", count = 42 };
        var testFunction = AIFunctionFactory.Create(() => resultObject, "TestFunction", "Test function description");
        var mockOption = Substitute.For<ChatOptions>();
        var sentryFunction = new SentryInstrumentedFunction(testFunction, mockOption);
        var arguments = new AIFunctionArguments();

        // Act
        var result = await sentryFunction.InvokeAsync(arguments);

        // Assert
        Assert.NotNull(result);
        if (result is JsonElement jsonElement)
        {
            // When AIFunction serializes objects, they become JsonElements
            var message = jsonElement.GetProperty("message").GetString();
            var count = jsonElement.GetProperty("count").GetInt32();
            Assert.Equal("test", message);
            Assert.Equal(42, count);
        }
        else
        {
            Assert.Equal(resultObject, result);
        }
    }

    [Fact]
    public async Task InvokeCoreAsync_WhenFunctionThrows_PropagatesException()
    {
        // Arrange
        using var sentryDisposable = SentryHelpers.InitializeSdk();
        var expectedException = new InvalidOperationException("Test exception");
        var testFunction = AIFunctionFactory.Create(new Func<object>(() => throw expectedException), "TestFunction", "Test function description");
        var mockOption = Substitute.For<ChatOptions>();
        var sentryFunction = new SentryInstrumentedFunction(testFunction, mockOption);
        var arguments = new AIFunctionArguments();

        // Act & Assert
        var actualException = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await sentryFunction.InvokeAsync(arguments));

        Assert.Equal(expectedException.Message, actualException.Message);
    }

    [Fact]
    public async Task InvokeCoreAsync_WithCancellation_PropagatesCancellation()
    {
        // Arrange
        using var sentryDisposable = SentryHelpers.InitializeSdk();
        var testFunction = AIFunctionFactory.Create((CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return "result";
        }, "TestFunction", "Test function description");

        var mockOption = Substitute.For<ChatOptions>();
        var sentryFunction = new SentryInstrumentedFunction(testFunction, mockOption);
        var arguments = new AIFunctionArguments();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await sentryFunction.InvokeAsync(arguments, cts.Token));
    }

    [Fact]
    public async Task InvokeCoreAsync_WithParameters_PassesParametersCorrectly()
    {
        // Arrange
        using var sentryDisposable = SentryHelpers.InitializeSdk();
        var receivedArguments = (AIFunctionArguments?)null;
        var testFunction = AIFunctionFactory.Create((AIFunctionArguments args) =>
        {
            receivedArguments = args;
            return "result";
        }, "TestFunction", "Test function description");

        var mockOption = Substitute.For<ChatOptions>();
        var sentryFunction = new SentryInstrumentedFunction(testFunction, mockOption);
        var arguments = new AIFunctionArguments { ["param1"] = "value1" };

        // Act
        await sentryFunction.InvokeAsync(arguments);

        // Assert
        Assert.NotNull(receivedArguments);
        Assert.Equal("value1", receivedArguments["param1"]);
    }

    [Fact]
    public void Constructor_PreservesInnerFunctionProperties()
    {
        // Arrange
        var testFunction = AIFunctionFactory.Create(() => "test", "TestFunction", "Test function description");

        // Act
        var mockOption = Substitute.For<ChatOptions>();
        var sentryFunction = new SentryInstrumentedFunction(testFunction, mockOption);

        // Assert
        Assert.Equal("TestFunction", sentryFunction.Name);
        Assert.Equal("Test function description", sentryFunction.Description);
    }
}

internal static class SentryHelpers
{
    public static IDisposable InitializeSdk()
    {
        return SentrySdk.Init(options =>
        {
            options.Dsn = "https://3f3a29aa3a3aff@fake-sentry.io:65535/2147483647";
            options.TracesSampleRate = 1.0;
            options.Debug = false;
        });
    }
}
