using Microsoft.Extensions.AI;
using Sentry.Internal;

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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task InvokeCoreAsync_OutsideToolLoopAndDataCollectionNotConfigured_RecordsRegardlessOfSendDefaultPii(
        bool sendDefaultPii)
    {
        // Arrange
        using var hub = CreateHub(options => options.SendDefaultPii = sendDefaultPii);
        var transaction = StartTransaction(hub);
        var sentryFunction = new SentryInstrumentedFunction(WeatherFunction(), hub);

        // Act
        await sentryFunction.InvokeAsync(new AIFunctionArguments { ["city"] = "Paris" });

        // Assert
        var toolSpan = transaction.Spans.Single(s => s.Operation == SentryAIConstants.SpanAttributes.ToolCallOperation);
        toolSpan.Data.Should().ContainKey(SentryAIConstants.SpanAttributes.ToolName).WhoseValue.Should().Be("GetWeather");
        toolSpan.Data.Should().ContainKey(SentryAIConstants.SpanAttributes.ToolDescription);
        toolSpan.Data.Should().ContainKey(SentryAIConstants.SpanAttributes.ToolInput);
        toolSpan.Data.Should().ContainKey(SentryAIConstants.SpanAttributes.ToolOutput);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task InvokeCoreAsync_OutsideToolLoop_FollowsDataCollection(bool inputs, bool outputs)
    {
        // Arrange
        using var hub = CreateHub(options =>
        {
            options.SendDefaultPii = !inputs;
            options.ConfigureDataCollection(dataCollection =>
            {
                dataCollection.GenAI.Inputs = inputs;
                dataCollection.GenAI.Outputs = outputs;
            });
        });
        var transaction = StartTransaction(hub);
        var sentryFunction = new SentryInstrumentedFunction(WeatherFunction(), hub);

        // Act
        await sentryFunction.InvokeAsync(new AIFunctionArguments { ["city"] = "Paris" });

        // Assert
        var toolSpan = transaction.Spans.Single(s => s.Operation == SentryAIConstants.SpanAttributes.ToolCallOperation);
        toolSpan.Data.Should().ContainKey(SentryAIConstants.SpanAttributes.ToolName);
        toolSpan.Data.ContainsKey(SentryAIConstants.SpanAttributes.ToolDescription).Should().Be(inputs);
        toolSpan.Data.ContainsKey(SentryAIConstants.SpanAttributes.ToolInput).Should().Be(inputs);
        toolSpan.Data.ContainsKey(SentryAIConstants.SpanAttributes.ToolOutput).Should().Be(outputs);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task InvokeCoreAsync_InsideToolLoopWithDataCollection_ChatClientRecordingOptionsTakePrecedence(
        bool recordInputs, bool recordOutputs)
    {
        // Arrange
        using var hub = CreateHub(options => options.ConfigureDataCollection(dataCollection =>
        {
            dataCollection.GenAI.Inputs = !recordInputs;
            dataCollection.GenAI.Outputs = !recordOutputs;
        }));
        var transaction = StartTransaction(hub);
        using var ficcActivity = StartFICCActivity(hub);

        var innerClient = Substitute.For<IChatClient>();
        innerClient.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>())
            .Returns(new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok")));
        var chatClient = new SentryChatClient(null, hub, innerClient, options =>
        {
            options.Experimental.RecordInputs = recordInputs;
            options.Experimental.RecordOutputs = recordOutputs;
        });
        var sentryFunction = new SentryInstrumentedFunction(WeatherFunction(), hub);

        // Act
        await chatClient.GetResponseAsync([new ChatMessage(ChatRole.User, "hi")], new ChatOptions { Tools = [sentryFunction] });
        await sentryFunction.InvokeAsync(new AIFunctionArguments { ["city"] = "Paris" });

        // Assert
        var toolSpan = transaction.Spans.Single(s => s.Operation == SentryAIConstants.SpanAttributes.ToolCallOperation);
        toolSpan.Data.Should().ContainKey(SentryAIConstants.SpanAttributes.ToolName);
        toolSpan.Data.ContainsKey(SentryAIConstants.SpanAttributes.ToolDescription).Should().Be(recordInputs);
        toolSpan.Data.ContainsKey(SentryAIConstants.SpanAttributes.ToolInput).Should().Be(recordInputs);
        toolSpan.Data.ContainsKey(SentryAIConstants.SpanAttributes.ToolOutput).Should().Be(recordOutputs);
    }

    [Fact]
    public async Task InvokeCoreAsync_InsideToolLoopWithoutDataCollection_RecordsRegardlessOfRecordingOptions()
    {
        // Arrange
        using var hub = CreateHub(_ => { });
        var transaction = StartTransaction(hub);
        using var ficcActivity = StartFICCActivity(hub);

        var innerClient = Substitute.For<IChatClient>();
        innerClient.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>())
            .Returns(new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok")));
        var chatClient = new SentryChatClient(null, hub, innerClient, options =>
        {
            options.Experimental.RecordInputs = false;
            options.Experimental.RecordOutputs = false;
        });
        var sentryFunction = new SentryInstrumentedFunction(WeatherFunction(), hub);

        // Act
        await chatClient.GetResponseAsync([new ChatMessage(ChatRole.User, "hi")], new ChatOptions { Tools = [sentryFunction] });
        await sentryFunction.InvokeAsync(new AIFunctionArguments { ["city"] = "Paris" });

        // Assert
        var toolSpan = transaction.Spans.Single(s => s.Operation == SentryAIConstants.SpanAttributes.ToolCallOperation);
        toolSpan.Data.Should().ContainKey(SentryAIConstants.SpanAttributes.ToolDescription);
        toolSpan.Data.Should().ContainKey(SentryAIConstants.SpanAttributes.ToolInput);
        toolSpan.Data.Should().ContainKey(SentryAIConstants.SpanAttributes.ToolOutput);
    }

    [Fact]
    public async Task InvokeCoreAsync_InChildActivityOfToolLoop_UsesChatClientRecordingOptions()
    {
        // Arrange
        using var hub = CreateHub(options => options.ConfigureDataCollection(_ => { }));
        var transaction = StartTransaction(hub);
        using var ficcActivity = StartFICCActivity(hub);

        var innerClient = Substitute.For<IChatClient>();
        innerClient.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>())
            .Returns(new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok")));
        var chatClient = new SentryChatClient(null, hub, innerClient, options =>
        {
            options.Experimental.RecordInputs = false;
            options.Experimental.RecordOutputs = false;
        });
        var sentryFunction = new SentryInstrumentedFunction(WeatherFunction(), hub);
        await chatClient.GetResponseAsync([new ChatMessage(ChatRole.User, "hi")], new ChatOptions { Tools = [sentryFunction] });

        // Act
        using (new Activity("execute_tool GetWeather").Start())
        {
            await sentryFunction.InvokeAsync(new AIFunctionArguments { ["city"] = "Paris" });
        }

        // Assert
        var toolSpan = transaction.Spans.Single(s => s.Operation == SentryAIConstants.SpanAttributes.ToolCallOperation);
        toolSpan.ParentSpanId.Should().Be(ficcActivity.GetFICCSpan()!.SpanId);
        toolSpan.Data.Should().NotContainKey(SentryAIConstants.SpanAttributes.ToolInput);
        toolSpan.Data.Should().NotContainKey(SentryAIConstants.SpanAttributes.ToolOutput);
    }

    private static AIFunction WeatherFunction() =>
        AIFunctionFactory.Create((string city) => $"Sunny in {city}", "GetWeather", "Gets the weather for a city");

    private static Hub CreateHub(Action<SentryOptions> configure)
    {
        var options = new SentryOptions { Dsn = ValidDsn, TracesSampleRate = 1.0 };
        configure(options);
        return new Hub(options);
    }

    private static ITransactionTracer StartTransaction(IHub hub)
    {
        var transaction = hub.StartTransaction("test-transaction", "test");
        hub.ConfigureScope(scope => scope.Transaction = transaction);
        return transaction;
    }

    // Started without an ActivitySource, so listeners registered by other tests can't replace the fused agent span
    private static Activity StartFICCActivity(IHub hub)
    {
        var agentSpan = hub.StartSpan(SentryAIConstants.SpanAttributes.InvokeAgentOperation,
            SentryAIConstants.SpanAttributes.InvokeAgentDescription);
        var activity = new Activity(SentryAIConstants.FICCActivityNames[0]).Start();
        activity.SetFused(SentryAIConstants.SentryFICCSpanAttributeName, agentSpan);
        return activity;
    }
}
