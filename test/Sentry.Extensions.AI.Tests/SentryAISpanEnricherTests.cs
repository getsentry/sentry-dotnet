using Microsoft.Extensions.AI;

namespace Sentry.Extensions.AI.Tests;

public class SentryAISpanEnricherTests : IDisposable
{
    private class Fixture
    {
        public SentryOptions Options { get; }
        public ISentryClient Client { get; }
        public Hub Hub { get; }

        public Fixture()
        {
            Options = new SentryOptions
            {
                Dsn = ValidDsn,
                TracesSampleRate = 1.0,
            };

            Hub = new Hub(Options);
            Client = Substitute.For<ISentryClient>();
        }
    }

    private readonly Fixture _fixture = new();

    public void Dispose()
    {
        _fixture.Hub.Dispose();
    }

    private static ChatMessage[] TestMessages()
    {
        var initialMessage = new ChatMessage(ChatRole.User, "Hello");

        return [initialMessage];
    }

    private static ChatOptions TestChatOptions()
    {
        return new ChatOptions()
        {
            ModelId = "SentryAI",
            Tools = new List<AITool>
            {
                AIFunctionFactory.Create((string? s) => Console.WriteLine(s), "SomeAIFunction",
                    "SomeAIFunctionDescription")
            },
            Temperature = 0.7f,
            MaxOutputTokens = 1024,
            TopP = 0.9f,
            FrequencyPenalty = 0.5f,
            PresencePenalty = 0.3f
        };
    }

    [Fact]
    public void EnrichWithRequest_SetsData()
    {
        // Arrange
        const string spanOp = "test_operation";
        const string spanDesc = "test_description";
        var span = _fixture.Hub.StartSpan(spanOp, spanDesc);
        var messages = TestMessages();
        var chatOptions = TestChatOptions();
        var aiOptions = new SentryAIOptions();

        // Act
        SentryAISpanEnricher.EnrichWithRequest(span, messages, chatOptions, aiOptions, _fixture.Options, SentryAIConstants.SpanOperations.Chat);

        // Assert
        span.Data.Should().ContainKey(SentryAIConstants.SpanAttributes.OperationName).WhoseValue.Should().Be(SentryAIConstants.SpanOperations.Chat);
        span.Data.Should().ContainKey(SentryAIConstants.SpanAttributes.RequestModel).WhoseValue.Should().Be("SentryAI");
        span.Data.Should().ContainKey(SentryAIConstants.SpanAttributes.RequestTemperature).WhoseValue.Should().Be(0.7f);
        span.Data.Should().ContainKey(SentryAIConstants.SpanAttributes.RequestMaxTokens).WhoseValue.Should().Be(1024);
        span.Data.Should().ContainKey(SentryAIConstants.SpanAttributes.RequestTopP).WhoseValue.Should().Be(0.9f);
        span.Data.Should().ContainKey(SentryAIConstants.SpanAttributes.RequestFrequencyPenalty).WhoseValue.Should().Be(0.5f);
        span.Data.Should().ContainKey(SentryAIConstants.SpanAttributes.RequestPresencePenalty).WhoseValue.Should().Be(0.3f);
        span.Data.Should().ContainKey(SentryAIConstants.SpanAttributes.RequestMessages).WhoseValue.Should().NotBeNull();
        span.Data.Should().ContainKey(SentryAIConstants.SpanAttributes.RequestAvailableTools).WhoseValue.Should().NotBeNull();
    }

    [Fact]
    public void EnrichWithRequest_SetsData_WithoutRequestMessages_WhenDisabled()
    {
        // Arrange
        const string spanOp = "test_operation";
        const string spanDesc = "test_description";
        var span = _fixture.Hub.StartSpan(spanOp, spanDesc);
        var messages = TestMessages();
        var chatOptions = TestChatOptions();
        var aiOptions = new SentryAIOptions()
        {
            Experimental =
            {
                RecordInputs = false
            }
        };

        // Act
        SentryAISpanEnricher.EnrichWithRequest(span, messages, chatOptions, aiOptions, _fixture.Options, SentryAIConstants.SpanOperations.Chat);

        // Assert
        span.Data.Should().ContainKey(SentryAIConstants.SpanAttributes.OperationName).WhoseValue.Should().Be(SentryAIConstants.SpanOperations.Chat);
        span.Data.Should().ContainKey(SentryAIConstants.SpanAttributes.RequestModel).WhoseValue.Should().Be("SentryAI");
        span.Data.Should().ContainKey(SentryAIConstants.SpanAttributes.RequestTemperature).WhoseValue.Should().Be(0.7f);
        span.Data.Should().ContainKey(SentryAIConstants.SpanAttributes.RequestMaxTokens).WhoseValue.Should().Be(1024);
        span.Data.Should().ContainKey(SentryAIConstants.SpanAttributes.RequestTopP).WhoseValue.Should().Be(0.9f);
        span.Data.Should().ContainKey(SentryAIConstants.SpanAttributes.RequestFrequencyPenalty).WhoseValue.Should().Be(0.5f);
        span.Data.Should().ContainKey(SentryAIConstants.SpanAttributes.RequestPresencePenalty).WhoseValue.Should().Be(0.3f);
        span.Data.Should().NotContainKey(SentryAIConstants.SpanAttributes.RequestMessages);
        span.Data.Should().NotContainKey(SentryAIConstants.SpanAttributes.RequestAvailableTools);
    }


    [Fact]
    public void EnrichWithRequest_SetsBasicData_WhenChatOptionsNull()
    {
        // Arrange
        const string spanOp = "test_operation";
        const string spanDesc = "test_description";
        var span = _fixture.Hub.StartSpan(spanOp, spanDesc);
        var messages = TestMessages();
        var aiOptions = new SentryAIOptions()
        {
            Experimental =
            {
                RecordInputs = false
            }
        };

        // Act
        SentryAISpanEnricher.EnrichWithRequest(span, messages, null, aiOptions, _fixture.Options, SentryAIConstants.SpanOperations.Chat);

        // Assert
        span.Data.Should().ContainKey(SentryAIConstants.SpanAttributes.OperationName).WhoseValue.Should().Be(SentryAIConstants.SpanOperations.Chat);
        span.Data.Should().NotContainKey(SentryAIConstants.SpanAttributes.RequestModel);
        span.Data.Should().NotContainKey(SentryAIConstants.SpanAttributes.RequestTemperature);
        span.Data.Should().NotContainKey(SentryAIConstants.SpanAttributes.RequestMaxTokens);
        span.Data.Should().NotContainKey(SentryAIConstants.SpanAttributes.RequestTopP);
        span.Data.Should().NotContainKey(SentryAIConstants.SpanAttributes.RequestFrequencyPenalty);
        span.Data.Should().NotContainKey(SentryAIConstants.SpanAttributes.RequestPresencePenalty);
        span.Data.Should().NotContainKey(SentryAIConstants.SpanAttributes.RequestMessages);
        span.Data.Should().NotContainKey(SentryAIConstants.SpanAttributes.RequestAvailableTools);
    }

    [Fact]
    public void ToolCallSpan_EnrichWithResponse_SetsData()
    {
        // Arrange
        var transaction = _fixture.Hub.StartTransaction("test_transaction", "test");
        var span = transaction.StartChild(SentryAIConstants.SpanAttributes.ChatOperation, "test_desc");
        var response = new ChatResponse([
            new ChatMessage(ChatRole.Assistant, [
                new TextContent("Hello"),
                new FunctionCallContent("test-call-id", "TestFunction", new Dictionary<string, object?> { ["param"] = "value" })
            ])
        ])
        {
            ModelId = "response-model-id",
            Usage = new UsageDetails
            {
                InputTokenCount = 50,
                OutputTokenCount = 25
            },
            FinishReason = ChatFinishReason.ToolCalls
        };
        var aiOptions = new SentryAIOptions();

        // Act
        SentryAISpanEnricher.EnrichWithResponse(span, response, aiOptions, _fixture.Options);

        // Assert
        span.Data.Should().ContainKey(SentryAIConstants.SpanAttributes.ResponseText).WhoseValue.Should().Be("Hello");
        span.Data.Should().ContainKey(SentryAIConstants.SpanAttributes.ResponseModel).WhoseValue.Should().Be("response-model-id");
        span.Data.Should().ContainKey(SentryAIConstants.SpanAttributes.UsageInputTokens).WhoseValue.Should().Be(50L);
        span.Data.Should().ContainKey(SentryAIConstants.SpanAttributes.UsageOutputTokens).WhoseValue.Should().Be(25L);
        span.Data.Should().ContainKey(SentryAIConstants.SpanAttributes.UsageTotalTokens).WhoseValue.Should().Be(75L);
        span.Data.Should().ContainKey(SentryAIConstants.SpanAttributes.ResponseToolCalls).WhoseValue.Should().NotBeNull();
    }
    [Fact]
    public void EnrichWithResponse_SetsData_WithoutResponseMessages_WhenDisabled()
    {
        // Arrange
        const string spanOp = SentryAIConstants.SpanAttributes.ChatOperation;
        const string spanDesc = "test_description";
        var span = _fixture.Hub.StartSpan(spanOp, spanDesc);
        var response = new ChatResponse(TestMessages())
        {
            ModelId = "response-model-id",
            Usage = new UsageDetails
            {
                InputTokenCount = 50,
                OutputTokenCount = 25
            }
        };
        var aiOptions = new SentryAIOptions()
        {
            Experimental =
            {
                RecordOutputs = false
            }
        };

        // Act
        SentryAISpanEnricher.EnrichWithResponse(span, response, aiOptions, _fixture.Options);

        // Assert
        span.Data.Should().NotContainKey(SentryAIConstants.SpanAttributes.ResponseText);
        span.Data.Should().ContainKey(SentryAIConstants.SpanAttributes.ResponseModel).WhoseValue.Should().Be("response-model-id");
        span.Data.Should().ContainKey(SentryAIConstants.SpanAttributes.UsageInputTokens).WhoseValue.Should().Be(50);
        span.Data.Should().ContainKey(SentryAIConstants.SpanAttributes.UsageOutputTokens).WhoseValue.Should().Be(25);
        span.Data.Should().ContainKey(SentryAIConstants.SpanAttributes.UsageTotalTokens).WhoseValue.Should().Be(75);
    }

    [Fact]
    public void EnrichWithStreamingResponses_SetsData()
    {
        // Arrange
        var transaction = _fixture.Hub.StartTransaction("test_transaction", "test");
        var span = transaction.StartChild(SentryAIConstants.SpanAttributes.ChatOperation, "test_desc");

        var streamingMessages = new List<ChatResponseUpdate>
        {
            new()
            {
                Contents = [
                    new TextContent("Hello "),
                    new UsageContent(new UsageDetails { InputTokenCount = 10, OutputTokenCount = 5 })
                ]
            },
            new()
            {
                ModelId = "streaming-model-id",
                Contents = [
                    new TextContent("world!"),
                    new UsageContent(new UsageDetails { InputTokenCount = 15, OutputTokenCount = 8 })
                ]
            },
            new()
            {
                FinishReason = ChatFinishReason.ToolCalls,
                Contents = [new FunctionCallContent("test-call-id", "TestFunction", new Dictionary<string, object?> { ["param"] = "value" })]
            }
        };

        var aiOptions = new SentryAIOptions { Experimental = { RecordOutputs = true } };

        // Act
        SentryAISpanEnricher.EnrichWithStreamingResponses(span, streamingMessages, aiOptions, _fixture.Options);

        // Assert
        span.Data.Should().ContainKey(SentryAIConstants.SpanAttributes.ResponseText).WhoseValue.Should().Be("Hello world!");
        span.Data.Should().ContainKey(SentryAIConstants.SpanAttributes.ResponseModel).WhoseValue.Should().Be("streaming-model-id");
        span.Data.Should().ContainKey(SentryAIConstants.SpanAttributes.UsageInputTokens).WhoseValue.Should().Be(25L);
        span.Data.Should().ContainKey(SentryAIConstants.SpanAttributes.UsageOutputTokens).WhoseValue.Should().Be(13L);
        span.Data.Should().ContainKey(SentryAIConstants.SpanAttributes.UsageTotalTokens).WhoseValue.Should().Be(38L);
        span.Data.Should().ContainKey(SentryAIConstants.SpanAttributes.ResponseToolCalls).WhoseValue.Should().NotBeNull();
    }

    [Fact]
    public void EnrichWithStreamingResponses_SetsData_WithoutResponseContent_WhenDisabled()
    {
        // Arrange
        const string spanOp = "test_operation";
        const string spanDesc = "test_description";
        var span = _fixture.Hub.StartSpan(spanOp, spanDesc);

        var streamingMessages = new List<ChatResponseUpdate>
        {
            new()
            {
                ModelId = "streaming-model-id",
                Contents = [
                    new TextContent("Hello world!"),
                    new UsageContent(new UsageDetails { InputTokenCount = 20, OutputTokenCount = 10 })
                ]
            }
        };

        var aiOptions = new SentryAIOptions { Experimental = { RecordOutputs = false } };

        // Act
        SentryAISpanEnricher.EnrichWithStreamingResponses(span, streamingMessages, aiOptions, _fixture.Options);

        // Assert
        span.Data.Should().NotContainKey(SentryAIConstants.SpanAttributes.ResponseText);
        span.Data.Should().ContainKey(SentryAIConstants.SpanAttributes.ResponseModel).WhoseValue.Should().Be("streaming-model-id");
        span.Data.Should().ContainKey(SentryAIConstants.SpanAttributes.UsageInputTokens).WhoseValue.Should().Be(20L);
        span.Data.Should().ContainKey(SentryAIConstants.SpanAttributes.UsageOutputTokens).WhoseValue.Should().Be(10L);
        span.Data.Should().ContainKey(SentryAIConstants.SpanAttributes.UsageTotalTokens).WhoseValue.Should().Be(30L);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EnrichWithRequest_DataCollectionNotConfigured_RecordsInputsRegardlessOfSendDefaultPii(bool sendDefaultPii)
    {
        // Arrange
        _fixture.Options.SendDefaultPii = sendDefaultPii;
        var span = _fixture.Hub.StartSpan("test_operation", "test_description");

        // Act
        SentryAISpanEnricher.EnrichWithRequest(span, TestMessages(), TestChatOptions(), new SentryAIOptions(),
            _fixture.Options, SentryAIConstants.SpanOperations.Chat);

        // Assert
        span.Data.Should().ContainKey(SentryAIConstants.SpanAttributes.RequestMessages);
        span.Data.Should().ContainKey(SentryAIConstants.SpanAttributes.RequestAvailableTools);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EnrichWithRequest_RecordInputsNotSet_FollowsDataCollection(bool inputs)
    {
        // Arrange
        _fixture.Options.SendDefaultPii = !inputs;
        _fixture.Options.ConfigureDataCollection(dataCollection => dataCollection.GenAI.Inputs = inputs);
        var span = _fixture.Hub.StartSpan("test_operation", "test_description");

        // Act
        SentryAISpanEnricher.EnrichWithRequest(span, TestMessages(), TestChatOptions(), new SentryAIOptions(),
            _fixture.Options, SentryAIConstants.SpanOperations.Chat);

        // Assert
        span.Data.ContainsKey(SentryAIConstants.SpanAttributes.RequestMessages).Should().Be(inputs);
        span.Data.ContainsKey(SentryAIConstants.SpanAttributes.RequestAvailableTools).Should().Be(inputs);
        span.Data.Should().ContainKey(SentryAIConstants.SpanAttributes.RequestModel);
        span.Data.Should().ContainKey(SentryAIConstants.SpanAttributes.AgentName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EnrichWithRequest_RecordInputsSet_TakesPrecedenceOverDataCollection(bool recordInputs)
    {
        // Arrange
        _fixture.Options.ConfigureDataCollection(dataCollection => dataCollection.GenAI.Inputs = !recordInputs);
        var span = _fixture.Hub.StartSpan("test_operation", "test_description");
        var aiOptions = new SentryAIOptions { Experimental = { RecordInputs = recordInputs } };

        // Act
        SentryAISpanEnricher.EnrichWithRequest(span, TestMessages(), TestChatOptions(), aiOptions,
            _fixture.Options, SentryAIConstants.SpanOperations.Chat);

        // Assert
        span.Data.ContainsKey(SentryAIConstants.SpanAttributes.RequestMessages).Should().Be(recordInputs);
        span.Data.ContainsKey(SentryAIConstants.SpanAttributes.RequestAvailableTools).Should().Be(recordInputs);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EnrichWithResponse_DataCollectionNotConfigured_RecordsOutputsRegardlessOfSendDefaultPii(bool sendDefaultPii)
    {
        // Arrange
        _fixture.Options.SendDefaultPii = sendDefaultPii;
        var transaction = _fixture.Hub.StartTransaction("test_transaction", "test");
        var span = transaction.StartChild(SentryAIConstants.SpanAttributes.ChatOperation, "test_desc");

        // Act
        SentryAISpanEnricher.EnrichWithResponse(span, ToolCallResponse(), new SentryAIOptions(), _fixture.Options);

        // Assert
        span.Data.Should().ContainKey(SentryAIConstants.SpanAttributes.ResponseText);
        span.Data.Should().ContainKey(SentryAIConstants.SpanAttributes.ResponseToolCalls);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EnrichWithResponse_RecordOutputsNotSet_FollowsDataCollection(bool outputs)
    {
        // Arrange
        _fixture.Options.SendDefaultPii = !outputs;
        _fixture.Options.ConfigureDataCollection(dataCollection => dataCollection.GenAI.Outputs = outputs);
        var transaction = _fixture.Hub.StartTransaction("test_transaction", "test");
        var span = transaction.StartChild(SentryAIConstants.SpanAttributes.ChatOperation, "test_desc");

        // Act
        SentryAISpanEnricher.EnrichWithResponse(span, ToolCallResponse(), new SentryAIOptions(), _fixture.Options);

        // Assert
        span.Data.ContainsKey(SentryAIConstants.SpanAttributes.ResponseText).Should().Be(outputs);
        span.Data.ContainsKey(SentryAIConstants.SpanAttributes.ResponseToolCalls).Should().Be(outputs);
        span.Data.Should().ContainKey(SentryAIConstants.SpanAttributes.ResponseModel);
        span.Data.Should().ContainKey(SentryAIConstants.SpanAttributes.UsageTotalTokens);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EnrichWithResponse_RecordOutputsSet_TakesPrecedenceOverDataCollection(bool recordOutputs)
    {
        // Arrange
        _fixture.Options.ConfigureDataCollection(dataCollection => dataCollection.GenAI.Outputs = !recordOutputs);
        var transaction = _fixture.Hub.StartTransaction("test_transaction", "test");
        var span = transaction.StartChild(SentryAIConstants.SpanAttributes.ChatOperation, "test_desc");
        var aiOptions = new SentryAIOptions { Experimental = { RecordOutputs = recordOutputs } };

        // Act
        SentryAISpanEnricher.EnrichWithResponse(span, ToolCallResponse(), aiOptions, _fixture.Options);

        // Assert
        span.Data.ContainsKey(SentryAIConstants.SpanAttributes.ResponseText).Should().Be(recordOutputs);
        span.Data.ContainsKey(SentryAIConstants.SpanAttributes.ResponseToolCalls).Should().Be(recordOutputs);
    }

    [Fact]
    public void EnrichWithResponse_RecordOutputsDisabled_OmitsToolCalls()
    {
        // Arrange
        var transaction = _fixture.Hub.StartTransaction("test_transaction", "test");
        var span = transaction.StartChild(SentryAIConstants.SpanAttributes.ChatOperation, "test_desc");
        var aiOptions = new SentryAIOptions { Experimental = { RecordOutputs = false } };

        // Act
        SentryAISpanEnricher.EnrichWithResponse(span, ToolCallResponse(), aiOptions, _fixture.Options);

        // Assert
        span.Data.Should().NotContainKey(SentryAIConstants.SpanAttributes.ResponseText);
        span.Data.Should().NotContainKey(SentryAIConstants.SpanAttributes.ResponseToolCalls);
    }

    private static ChatResponse ToolCallResponse() =>
        new([
            new ChatMessage(ChatRole.Assistant, [
                new TextContent("Hello"),
                new FunctionCallContent("test-call-id", "TestFunction", new Dictionary<string, object?> { ["param"] = "value" })
            ])
        ])
        {
            ModelId = "response-model-id",
            Usage = new UsageDetails { InputTokenCount = 50, OutputTokenCount = 25 },
            FinishReason = ChatFinishReason.ToolCalls
        };
}
