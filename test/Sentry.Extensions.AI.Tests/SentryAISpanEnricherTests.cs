#nullable enable
using Microsoft.Extensions.AI;

namespace Sentry.Extensions.AI.Tests;

public class SentryAISpanEnricherTests
{
    private class Fixture
    {
        private SentryOptions Options { get; }
        public ISentryClient Client { get; }
        public Hub Hub { get; set; }

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
        SentryAISpanEnricher.EnrichWithRequest(span, messages, chatOptions, aiOptions, SentryAIConstants.SpanOperations.Chat);

        // Assert
        span.Data[SentryAIConstants.SpanAttributes.OperationName].Should().Be(SentryAIConstants.SpanOperations.Chat);
        span.Data[SentryAIConstants.SpanAttributes.RequestModel].Should().Be("SentryAI");
        span.Data[SentryAIConstants.SpanAttributes.RequestTemperature].Should().Be(0.7f);
        span.Data[SentryAIConstants.SpanAttributes.RequestMaxTokens].Should().Be(1024);
        span.Data[SentryAIConstants.SpanAttributes.RequestTopP].Should().Be(0.9f);
        span.Data[SentryAIConstants.SpanAttributes.RequestFrequencyPenalty].Should().Be(0.5f);
        span.Data[SentryAIConstants.SpanAttributes.RequestPresencePenalty].Should().Be(0.3f);
        span.Data[SentryAIConstants.SpanAttributes.RequestMessages].Should().NotBeNull();
        span.Data[SentryAIConstants.SpanAttributes.RequestAvailableTools].Should().NotBeNull();
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
            RecordInputs = false
        };

        // Act
        SentryAISpanEnricher.EnrichWithRequest(span, messages, chatOptions, aiOptions,  SentryAIConstants.SpanOperations.Chat);

        // Assert
        span.Data[SentryAIConstants.SpanAttributes.OperationName].Should().Be(SentryAIConstants.SpanOperations.Chat);
        span.Data[SentryAIConstants.SpanAttributes.RequestModel].Should().Be("SentryAI");
        span.Data[SentryAIConstants.SpanAttributes.RequestTemperature].Should().Be(0.7f);
        span.Data[SentryAIConstants.SpanAttributes.RequestMaxTokens].Should().Be(1024);
        span.Data[SentryAIConstants.SpanAttributes.RequestTopP].Should().Be(0.9f);
        span.Data[SentryAIConstants.SpanAttributes.RequestFrequencyPenalty].Should().Be(0.5f);
        span.Data[SentryAIConstants.SpanAttributes.RequestPresencePenalty].Should().Be(0.3f);
        span.Data.Should().NotContainKey(SentryAIConstants.SpanAttributes.RequestMessages);
        span.Data[SentryAIConstants.SpanAttributes.RequestAvailableTools].Should().NotBeNull();
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
            RecordInputs = false
        };

        // Act
        SentryAISpanEnricher.EnrichWithRequest(span, messages, null, aiOptions, SentryAIConstants.SpanOperations.Chat);

        // Assert
        span.Data[SentryAIConstants.SpanAttributes.OperationName].Should().Be(SentryAIConstants.SpanOperations.Chat);
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
    public void EnrichWithResponse_SetsData()
    {
        // Arrange
        const string spanOp = "test_operation";
        const string spanDesc = "test_description";
        var span = _fixture.Hub.StartSpan(spanOp, spanDesc);
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
        SentryAISpanEnricher.EnrichWithResponse(span, response, aiOptions);

        // Assert
        span.Data[SentryAIConstants.SpanAttributes.ResponseText].Should().Be("Hello");
        span.Data[SentryAIConstants.SpanAttributes.ResponseModel].Should().Be("response-model-id");
        span.Data[SentryAIConstants.SpanAttributes.UsageInputTokens].Should().Be(50L);
        span.Data[SentryAIConstants.SpanAttributes.UsageOutputTokens].Should().Be(25L);
        span.Data[SentryAIConstants.SpanAttributes.UsageTotalTokens].Should().Be(75L);
        span.Data[SentryAIConstants.SpanAttributes.ResponseToolCalls].Should().NotBeNull();
    }
    [Fact]
    public void EnrichWithResponse_SetsData_WithoutResponseMessages_WhenDisabled()
    {
        // Arrange
        const string spanOp = "test_operation";
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
            RecordOutputs = false
        };

        // Act
        SentryAISpanEnricher.EnrichWithResponse(span, response, aiOptions);

        // Assert
        span.Data.Should().NotContainKey(SentryAIConstants.SpanAttributes.ResponseText);
        span.Data[SentryAIConstants.SpanAttributes.ResponseModel].Should().Be("response-model-id");
        span.Data[SentryAIConstants.SpanAttributes.UsageInputTokens].Should().Be(50);
        span.Data[SentryAIConstants.SpanAttributes.UsageOutputTokens].Should().Be(25);
        span.Data[SentryAIConstants.SpanAttributes.UsageTotalTokens].Should().Be(75);
    }

    [Fact]
    public void EnrichWithStreamingResponses_SetsData()
    {
        // Arrange
        const string spanOp = "test_operation";
        const string spanDesc = "test_description";
        var span = _fixture.Hub.StartSpan(spanOp, spanDesc);

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

        var aiOptions = new SentryAIOptions { RecordOutputs = true };

        // Act
        SentryAISpanEnricher.EnrichWithStreamingResponses(span, streamingMessages, aiOptions);

        // Assert
        span.Data[SentryAIConstants.SpanAttributes.ResponseText].Should().Be("Hello world!");
        span.Data[SentryAIConstants.SpanAttributes.ResponseModel].Should().Be("streaming-model-id");
        span.Data[SentryAIConstants.SpanAttributes.UsageInputTokens].Should().Be(25L);
        span.Data[SentryAIConstants.SpanAttributes.UsageOutputTokens].Should().Be(13L);
        span.Data[SentryAIConstants.SpanAttributes.UsageTotalTokens].Should().Be(38L);
        span.Data[SentryAIConstants.SpanAttributes.ResponseToolCalls].Should().NotBeNull();
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

        var aiOptions = new SentryAIOptions { RecordOutputs = false };

        // Act
        SentryAISpanEnricher.EnrichWithStreamingResponses(span, streamingMessages, aiOptions);

        // Assert
        span.Data.Should().NotContainKey(SentryAIConstants.SpanAttributes.ResponseText);
        span.Data[SentryAIConstants.SpanAttributes.ResponseModel].Should().Be("streaming-model-id");
        span.Data[SentryAIConstants.SpanAttributes.UsageInputTokens].Should().Be(20L);
        span.Data[SentryAIConstants.SpanAttributes.UsageOutputTokens].Should().Be(10L);
        span.Data[SentryAIConstants.SpanAttributes.UsageTotalTokens].Should().Be(30L);
    }
}
