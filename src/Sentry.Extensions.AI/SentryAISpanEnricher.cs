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
    internal static void EnrichWithRequest(ISpan span, ChatMessage[] messages, ChatOptions? options,
        SentryAIOptions aiOptions)
    {
        // Currently, all spans will be "chat"
        span.SetData(SentryAIConstants.SpanAttributes.OperationName, "chat");

        if (options?.ModelId is { } modelId)
        {
            span.SetData(SentryAIConstants.SpanAttributes.RequestModel, modelId);
        }

        if (aiOptions?.AgentName is { } agentName)
        {
            span.SetData(SentryAIConstants.SpanAttributes.AgentName, agentName);
        }

        if (messages is { Length: > 0 } && (aiOptions?.IncludeAIRequestMessages ?? true))
        {
            span.SetData(SentryAIConstants.SpanAttributes.RequestMessages, FormatRequestMessage(messages));
        }

        if (options?.Tools is { } tools)
        {
            span.SetData(SentryAIConstants.SpanAttributes.RequestAvailableTools, FormatAvailableTools(tools));
        }

        if (options?.Temperature is { } temperature)
        {
            span.SetData(SentryAIConstants.SpanAttributes.RequestTemperature, temperature);
        }

        if (options?.MaxOutputTokens is { } maxOutputTokens)
        {
            span.SetData(SentryAIConstants.SpanAttributes.RequestMaxTokens, maxOutputTokens);
        }

        if (options?.TopP is { } topP)
        {
            span.SetData(SentryAIConstants.SpanAttributes.RequestTopP, topP);
        }

        if (options?.FrequencyPenalty is { } frequencyPenalty)
        {
            span.SetData(SentryAIConstants.SpanAttributes.RequestFrequencyPenalty, frequencyPenalty);
        }

        if (options?.PresencePenalty is { } presencePenalty)
        {
            span.SetData(SentryAIConstants.SpanAttributes.RequestPresencePenalty, presencePenalty);
        }
    }

    /// <summary>
    /// Enriches the span with response information.
    /// </summary>
    /// <remarks>
    /// This function converts a <see cref="ChatResponse"/> to a list of <see cref="ChatResponseUpdate"/>, then
    /// enriches the span with it.
    /// </remarks>
    /// <param name="span">Span to enrich</param>
    /// <param name="response">Chat response containing usage and content data</param>
    /// <param name="aiOptions">AI-specific options</param>
    internal static void EnrichWithResponse(ISpan span, ChatResponse response, SentryAIOptions aiOptions)
    {
        EnrichWithStreamingResponses(span, [.. response.ToChatResponseUpdates()], aiOptions);
    }

    /// <summary>
    /// Enriches the span using the list of streamed in <see cref="ChatResponseUpdate"/>.
    /// </summary>
    /// <param name="span">span to enrich</param>
    /// <param name="messages">a list of <see cref="ChatResponseUpdate"/></param>
    /// <param name="aiOptions">AI-specific options</param>
    public static void EnrichWithStreamingResponses(ISpan span, List<ChatResponseUpdate> messages,
        SentryAIOptions aiOptions)
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
                span.SetData(SentryAIConstants.SpanAttributes.ResponseModel, modelId);
            }

            if (message.Text is { } responseText)
            {
                finalText.Append(responseText);
            }

            if (message.FinishReason == ChatFinishReason.ToolCalls)
            {
                PopulateToolCallsInfo(message.Contents, span);
            }
        }

        if (aiOptions.IncludeAIResponseContent)
        {
            span.SetData(SentryAIConstants.SpanAttributes.ResponseText, finalText.ToString());
        }

        span.SetData(SentryAIConstants.SpanAttributes.UsageInputTokens, inputTokenCount);
        span.SetData(SentryAIConstants.SpanAttributes.UsageOutputTokens, outputTokenCount);
        span.SetData(SentryAIConstants.SpanAttributes.UsageTotalTokens, inputTokenCount + outputTokenCount);
    }

    private static void PopulateToolCallsInfo(IList<AIContent> contents, ISpan span)
    {
        var functionContents = contents.OfType<FunctionCallContent>().ToArray();
        if (functionContents.Length > 0)
        {
            span.SetData(SentryAIConstants.SpanAttributes.ResponseToolCalls,
                FormatFunctionCallContent(functionContents));
        }
    }

    private static string FormatAvailableTools(IList<AITool> tools)
    {
        try
        {
            var str = FormatAsJson(tools, tool => new
            {
                name = tool.Name,
                description = tool.Description
            });
            return str;
        }
        catch
        {
            return "";
        }
    }

    private static string FormatRequestMessage(ChatMessage[] messages)
    {
        try
        {
            var str = FormatAsJson(messages, message => new
            {
                role = message.Role,
                content = message.Text
            });
            return str;
        }
        catch
        {
            return "";
        }
    }

    private static string FormatFunctionCallContent(FunctionCallContent[] content) =>
        FormatAsJson(content, c => new
        {
            name = c.Name,
            type = "function_call",
            arguments = JsonSerializer.Serialize(c.Arguments)
        });

    private static string FormatAsJson<T>(IEnumerable<T> items, Func<T, object> selector) =>
        JsonSerializer.Serialize(items.Select(selector));
}
