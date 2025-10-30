#nullable enable
using System.Text.Json;
using Microsoft.Extensions.AI;
using NSubstitute;
using Sentry.Extensions.AI;

namespace Sentry.Extensions.AI.Tests;

public class SentryAISpanEnricherTests
{
    private readonly ISpan _mockSpan;

    public SentryAISpanEnricherTests()
    {
        _mockSpan = Substitute.For<ISpan>();
    }

    [Fact]
    public void EnrichWithRequest_SetsBasicOperationName()
    {
        var messages = new[] { new ChatMessage(ChatRole.User, "Hello") };

        SentryAISpanEnricher.EnrichWithRequest(_mockSpan, messages, null);

        _mockSpan.Received(1).SetData("gen_ai.operation.name", "chat");
    }

    [Fact]
    public void EnrichWithRequest_SetsModel_WhenProvided()
    {
        var messages = new[] { new ChatMessage(ChatRole.User, "Hello") };
        var options = new ChatOptions { ModelId = "gpt-4" };

        SentryAISpanEnricher.EnrichWithRequest(_mockSpan, messages, options);

        _mockSpan.Received(1).SetData("gen_ai.request.model", "gpt-4");
    }

    [Fact]
    public void EnrichWithRequest_DoesntSetModel_WhenModelIdNotProvided()
    {
        var messages = new[] { new ChatMessage(ChatRole.User, "Hello") };

        SentryAISpanEnricher.EnrichWithRequest(_mockSpan, messages, null);

        _mockSpan.DidNotReceive().SetData("gen_ai.request.model", Arg.Any<object>());
    }

    [Fact]
    public void EnrichWithRequest_SetsMessages_WhenIncludeRequestMessagesIsTrue()
    {
        var messages = new[] {
            new ChatMessage(ChatRole.User, "Hello"),
            new ChatMessage(ChatRole.Assistant, "Hi there")
        };
        var aiOptions = new SentryAIOptions { IncludeAIRequestMessages = true };

        SentryAISpanEnricher.EnrichWithRequest(_mockSpan, messages, null, aiOptions);

        _mockSpan.Received(1).SetData("gen_ai.request.messages", Arg.Any<string>());
    }

    [Fact]
    public void EnrichWithRequest_DoesNotSetMessages_WhenIncludeRequestMessagesIsFalse()
    {
        var messages = new[] { new ChatMessage(ChatRole.User, "Hello") };
        var aiOptions = new SentryAIOptions { IncludeAIRequestMessages = false };

        SentryAISpanEnricher.EnrichWithRequest(_mockSpan, messages, null, aiOptions);

        _mockSpan.DidNotReceive().SetData("gen_ai.request.messages", Arg.Any<string>());
    }

    [Fact]
    public void EnrichWithRequest_SetsMessages_WhenAIOptionsIsNull()
    {
        var messages = new[] { new ChatMessage(ChatRole.User, "Hello") };

        SentryAISpanEnricher.EnrichWithRequest(_mockSpan, messages, null, null);

        _mockSpan.Received(1).SetData("gen_ai.request.messages", Arg.Any<string>());
    }

    [Fact]
    public void EnrichWithRequest_DoesNotSetMessages_WhenMessagesArrayIsEmpty()
    {
        var messages = Array.Empty<ChatMessage>();

        SentryAISpanEnricher.EnrichWithRequest(_mockSpan, messages, null);

        _mockSpan.DidNotReceive().SetData("gen_ai.request.messages", Arg.Any<string>());
    }

    [Fact]
    public void EnrichWithRequest_SetsTools_WhenToolsProvided()
    {
        var messages = new[] { new ChatMessage(ChatRole.User, "Hello") };
        var tools = new List<AITool>
        {
            AIFunctionFactory.Create(() => "test", "TestTool", "A test tool")
        };
        var options = new ChatOptions { Tools = tools };

        SentryAISpanEnricher.EnrichWithRequest(_mockSpan, messages, options);

        _mockSpan.Received(1).SetData("gen_ai.request.available_tools", Arg.Any<string>());
    }

    [Fact]
    public void EnrichWithRequest_SetsTemperature_WhenProvided()
    {
        var messages = new[] { new ChatMessage(ChatRole.User, "Hello") };
        var options = new ChatOptions { Temperature = 0.7f };

        SentryAISpanEnricher.EnrichWithRequest(_mockSpan, messages, options);

        _mockSpan.Received(1).SetData("gen_ai.request.temperature", 0.7f);
    }

    [Fact]
    public void EnrichWithRequest_SetsMaxOutputTokens_WhenProvided()
    {
        var messages = new[] { new ChatMessage(ChatRole.User, "Hello") };
        var options = new ChatOptions { MaxOutputTokens = 1000 };

        SentryAISpanEnricher.EnrichWithRequest(_mockSpan, messages, options);

        _mockSpan.Received(1).SetData("gen_ai.request.max_tokens", 1000);
    }

    [Fact]
    public void EnrichWithRequest_SetsTopP_WhenProvided()
    {
        var messages = new[] { new ChatMessage(ChatRole.User, "Hello") };
        var options = new ChatOptions { TopP = 0.9f };

        SentryAISpanEnricher.EnrichWithRequest(_mockSpan, messages, options);

        _mockSpan.Received(1).SetData("gen_ai.request.top_p", 0.9f);
    }

    [Fact]
    public void EnrichWithRequest_SetsFrequencyPenalty_WhenProvided()
    {
        var messages = new[] { new ChatMessage(ChatRole.User, "Hello") };
        var options = new ChatOptions { FrequencyPenalty = 0.5f };

        SentryAISpanEnricher.EnrichWithRequest(_mockSpan, messages, options);

        _mockSpan.Received(1).SetData("gen_ai.request.frequency_penalty", 0.5f);
    }

    [Fact]
    public void EnrichWithRequest_SetsPresencePenalty_WhenProvided()
    {
        var messages = new[] { new ChatMessage(ChatRole.User, "Hello") };
        var options = new ChatOptions { PresencePenalty = 0.3f };

        SentryAISpanEnricher.EnrichWithRequest(_mockSpan, messages, options);

        _mockSpan.Received(1).SetData("gen_ai.request.presence_penalty", 0.3f);
    }

    [Fact]
    public void EnrichWithResponse_SetsUsageTokens_WhenUsageProvided()
    {
        var usage = new UsageDetails { InputTokenCount = 10, OutputTokenCount = 20 };
        var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, "Hello"))
        {
            Usage = usage
        };

        SentryAISpanEnricher.EnrichWithResponse(_mockSpan, response);

        _mockSpan.Received(1).SetData("gen_ai.usage.input_tokens", 10L);
        _mockSpan.Received(1).SetData("gen_ai.usage.output_tokens", 20L);
        _mockSpan.Received(1).SetData("gen_ai.usage.total_tokens", 30L);
    }

    [Fact]
    public void EnrichWithResponse_DoesNotSetUsage_WhenUsageIsNull()
    {
        var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, "Hello"));

        SentryAISpanEnricher.EnrichWithResponse(_mockSpan, response);

        _mockSpan.DidNotReceive().SetData("gen_ai.usage.input_tokens", Arg.Any<int>());
        _mockSpan.DidNotReceive().SetData("gen_ai.usage.output_tokens", Arg.Any<int>());
        _mockSpan.DidNotReceive().SetData("gen_ai.usage.total_tokens", Arg.Any<int>());
    }

    [Fact]
    public void EnrichWithResponse_SetsPartialTokens_WhenOnlyInputTokensProvided()
    {
        var usage = new UsageDetails { InputTokenCount = 10 };
        var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, "Hello"))
        {
            Usage = usage
        };

        SentryAISpanEnricher.EnrichWithResponse(_mockSpan, response);

        _mockSpan.Received().SetData("gen_ai.usage.input_tokens", 10L);
        _mockSpan.DidNotReceive().SetData("gen_ai.usage.output_tokens", Arg.Any<int>());
        _mockSpan.DidNotReceive().SetData("gen_ai.usage.total_tokens", Arg.Any<int>());
    }

    [Fact]
    public void EnrichWithResponse_SetsResponseText_WhenIncludeResponseContentIsTrue()
    {
        var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, "Hello world"));
        var aiOptions = new SentryAIOptions { IncludeAIResponseContent = true };

        SentryAISpanEnricher.EnrichWithResponse(_mockSpan, response, aiOptions);

        _mockSpan.Received(1).SetData("gen_ai.response.text", "Hello world");
    }

    [Fact]
    public void EnrichWithResponse_DoesNotSetResponseText_WhenIncludeResponseContentIsFalse()
    {
        var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, "Hello world"));
        var aiOptions = new SentryAIOptions { IncludeAIResponseContent = false };

        SentryAISpanEnricher.EnrichWithResponse(_mockSpan, response, aiOptions);

        _mockSpan.DidNotReceive().SetData("gen_ai.response.text", Arg.Any<string>());
    }

    [Fact]
    public void EnrichWithResponse_SetsResponseText_WhenAIOptionsIsNull()
    {
        var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, "Hello world"));

        SentryAISpanEnricher.EnrichWithResponse(_mockSpan, response, null);

        _mockSpan.Received(1).SetData("gen_ai.response.text", "Hello world");
    }

    [Fact]
    public void EnrichWithResponse_SetsModelId_WhenProvided()
    {
        var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, "Hello"))
        {
            ModelId = "gpt-4-turbo"
        };

        SentryAISpanEnricher.EnrichWithResponse(_mockSpan, response);

        _mockSpan.Received().SetData("gen_ai.response.model", "gpt-4-turbo");
    }

    [Fact]
    public void EnrichWithStreamingResponse_AccumulatesTokenUsage()
    {
        var messages = new List<ChatResponseUpdate>
        {
            new(ChatRole.Assistant, "Hello")
            {
                Contents = [new UsageContent(new UsageDetails { InputTokenCount = 5, OutputTokenCount = 10 })]
            },
            new(ChatRole.Assistant, " world")
            {
                Contents = [new UsageContent(new UsageDetails { InputTokenCount = 3, OutputTokenCount = 5 })]
            }
        };

        SentryAISpanEnricher.EnrichWithStreamingResponses(_mockSpan, messages);

        _mockSpan.Received(1).SetData("gen_ai.usage.input_tokens", 8L);
        _mockSpan.Received(1).SetData("gen_ai.usage.output_tokens", 15L);
        _mockSpan.Received(1).SetData("gen_ai.usage.total_tokens", 23L);
    }

    [Fact]
    public void EnrichWithStreamingResponse_ConcatenatesResponseText()
    {
        var messages = new List<ChatResponseUpdate>
        {
            new(ChatRole.Assistant, "Hello"),
            new(ChatRole.Assistant, " world"),
            new(ChatRole.Assistant, "!")
        };

        SentryAISpanEnricher.EnrichWithStreamingResponses(_mockSpan, messages);

        _mockSpan.Received(1).SetData("gen_ai.response.text", "Hello world!");
    }

    [Fact]
    public void EnrichWithStreamingResponse_DoesNotSetResponseText_WhenIncludeResponseContentIsFalse()
    {
        var messages = new List<ChatResponseUpdate>
        {
            new(ChatRole.Assistant, "Hello world")
        };
        var aiOptions = new SentryAIOptions { IncludeAIResponseContent = false };

        SentryAISpanEnricher.EnrichWithStreamingResponses(_mockSpan, messages, aiOptions);

        _mockSpan.DidNotReceive().SetData("gen_ai.response.text", Arg.Any<string>());
    }

    [Fact]
    public void EnrichWithStreamingResponse_SetsModelId_FromLastMessageWithModelId()
    {
        var messages = new List<ChatResponseUpdate>
        {
            new(ChatRole.Assistant, "Hello") { ModelId = "gpt-3.5" },
            new(ChatRole.Assistant, " world") { ModelId = "gpt-4" },
            new(ChatRole.Assistant, "!")
        };

        SentryAISpanEnricher.EnrichWithStreamingResponses(_mockSpan, messages);

        _mockSpan.Received(1).SetData("gen_ai.response.model_id", "gpt-4");
    }
}
