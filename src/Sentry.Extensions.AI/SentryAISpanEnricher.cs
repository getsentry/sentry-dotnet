using System.Text.Json;
using Microsoft.Extensions.AI;

namespace Sentry.Extensions.AI;

/// <summary>
/// Populates various span attributes specific to AI
/// </summary>
internal static class SentryAISpanEnricher
{
    /// <summary>
    /// Enriches a span with request information.
    /// </summary>
    /// <param name="span">Span to enrich</param>
    /// <param name="messages">Messages</param>
    /// <param name="options">Options</param>
    /// <param name="aiOptions">AI-specific options</param>
    internal static void EnrichWithRequest(ISpan span, ChatMessage[] messages, ChatOptions? options, SentryAIOptions? aiOptions = null)
    {
        // Currently, all top-level spans will start as "chat"
        // The agent creation/invocation doesn't really work in Microsoft.Extensions.AI
        span.SetData("gen_ai.operation.name", "chat");

        if (options?.ModelId is { } modelId)
        {
            span.SetData("gen_ai.request.model", modelId);
        }

        if (aiOptions?.AgentName is { } agentName)
        {
            span.SetData("gen_ai.agent.name", agentName);
        }

        if (messages is { Length: > 0 } && (aiOptions?.IncludeAIRequestMessages ?? true))
        {
            span.SetData("gen_ai.request.messages", FormatRequestMessage(messages));
        }

        if (options?.Tools is { } tools)
        {
            span.SetData("gen_ai.request.available_tools", FormatAvailableTools(tools));
        }

        if (options?.Temperature is { } temperature)
        {
            span.SetData("gen_ai.request.temperature", temperature);
        }

        if (options?.MaxOutputTokens is { } maxOutputTokens)
        {
            span.SetData("gen_ai.request.max_tokens", maxOutputTokens);
        }

        if (options?.TopP is { } topP)
        {
            span.SetData("gen_ai.request.top_p", topP);
        }

        if (options?.FrequencyPenalty is { } frequencyPenalty)
        {
            span.SetData("gen_ai.request.frequency_penalty", frequencyPenalty);
        }

        if (options?.PresencePenalty is { } presencePenalty)
        {
            span.SetData("gen_ai.request.presence_penalty", presencePenalty);
        }
    }

    /// <summary>
    /// Enriches the span with response information.
    /// </summary>
    /// <param name="span">Span to enrich</param>
    /// <param name="response">Chat response containing usage and content data</param>
    /// <param name="aiOptions">AI-specific options</param>
    internal static void EnrichWithResponse(ISpan span, ChatResponse response, SentryAIOptions? aiOptions = null)
    {
        if (response.Usage is { } usage)
        {
            var inputTokens = usage.InputTokenCount;
            var outputTokens = usage.OutputTokenCount;

            if (inputTokens.HasValue)
            {
                span.SetData("gen_ai.usage.input_tokens", inputTokens.Value);
            }

            if (outputTokens.HasValue)
            {
                span.SetData("gen_ai.usage.output_tokens", outputTokens.Value);
            }

            if (inputTokens.HasValue && outputTokens.HasValue)
            {
                span.SetData("gen_ai.usage.total_tokens", inputTokens.Value + outputTokens.Value);
            }
        }

        if (response.Text is { } responseText && (aiOptions?.IncludeAIResponseContent ?? true))
        {
            span.SetData("gen_ai.response.text", responseText);
        }

        if (response.ModelId is { } modelId)
        {
            span.SetData("gen_ai.response.model", modelId);
        }
    }

    /// <summary>
    /// Enriches the span using the list of streamed in <see cref="ChatResponseUpdate"/>.
    /// </summary>
    /// <param name="span">span to enrich</param>
    /// <param name="messages">a list of <see cref="ChatResponseUpdate"/></param>
    /// <param name="aiOptions">AI-specific options</param>
    public static void EnrichWithStreamingResponse(ISpan span, List<ChatResponseUpdate> messages, SentryAIOptions? aiOptions = null)
    {
        var inputTokenCount = 0L;
        var outputTokenCount = 0L;
        var finalText = new StringBuilder();

        foreach (var message in messages)
        {
            foreach (var content in message.Contents)
            {
                if (content is UsageContent { } usage)
                {
                    inputTokenCount += usage.Details.InputTokenCount ?? 0;
                    outputTokenCount += usage.Details.OutputTokenCount ?? 0;
                }
            }
            if (message.ModelId is { } modelId)
            {
                span.SetData("gen_ai.response.model_id", modelId);
            }
            if (message.Text is { } responseText)
            {
                finalText.Append(responseText);
            }
        }

        if (aiOptions?.IncludeAIResponseContent ?? true)
        {
            span.SetData("gen_ai.response.text", finalText.ToString());
        }
        span.SetData("gen_ai.usage.input_tokens", inputTokenCount);
        span.SetData("gen_ai.usage.output_tokens", outputTokenCount);
        span.SetData("gen_ai.usage.total_tokens", inputTokenCount + outputTokenCount);
    }

    private static string FormatAvailableTools(IList<AITool> tools) =>
        FormatAsJson(tools, tool => new { name = tool.Name, description = tool.Description });

    private static string FormatRequestMessage(ChatMessage[] messages) =>
        FormatAsJson(messages, message => new { role = message.Role, content = message.Text });

    private static string FormatAsJson<T>(IEnumerable<T> items, Func<T, object> selector) =>
        JsonSerializer.Serialize(items.Select(selector));
}
