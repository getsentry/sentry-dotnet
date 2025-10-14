using Microsoft.Extensions.AI;

namespace Sentry.Extensions.AI;

/// <summary>
/// Populates various span attributes specific to AI
/// </summary>
public static class SentryAISpanEnricher
{
    /// <summary>
    /// Enrich a span with request information
    /// </summary>
    /// <param name="span">Span to enrich</param>
    /// <param name="messages">Messages</param>
    /// <param name="options">Options</param>
    /// <param name="agentName">Agent's name</param>
    /// <param name="system">The AI product (e.g. OpenAI, Anthropic, etc)</param>
    public static void EnrichWithRequest(ISpan span, ChatMessage[] messages, ChatOptions? options, string? agentName, string? system)
    {
        span.SetData("gen_ai.operation.name", "chat");
        if (system is { Length: > 0 })
        {
            span.SetData("gen_ai.system", system);
        }

        if (options?.ModelId is { } modelId)
        {
            span.SetData("gen_ai.request.model", modelId);
        }
        else
        {
            span.SetData("gen_ai.request.model", "Unknown model");
        }

        if (messages is { Length: > 0 })
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

        if (agentName is { Length: > 0 })
        {
            span.SetData("gen_ai.agent.name", agentName);
        }
    }

    /// <summary>
    /// Enriches the <param name="span">span</param> using the <param name="response">response</param>.
    /// </summary>
    public static void EnrichWithResponse(ISpan span, ChatResponse response)
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

        if (response.Text is { } responseText)
        {
            span.SetData("gen_ai.response.text", responseText);
        }

        if (response.ModelId is { } modelId)
        {
            span.SetData("gen_ai.response.model_id", modelId);
        }
    }

    private static string FormatAvailableTools(IList<AITool> tools) =>
        FormatAsJson(tools, tool => new { name = tool.Name, description = tool.Description });

    private static string FormatRequestMessage(ChatMessage[] messages) =>
        FormatAsJson(messages, message => new { role = message.Role, content = message.Text });

    private static string FormatAsJson<T>(IEnumerable<T> items, Func<T, object> selector) =>
        JsonSerializer.Serialize(items.Select(selector));
}
