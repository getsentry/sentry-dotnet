using Microsoft.Extensions.AI;
using Sentry.Extensibility;

namespace Sentry.Extensions.AI;

internal sealed class SentryChatClient : DelegatingChatClient
{
    private readonly HubAdapter _hub;
    private readonly SentryAIOptions _sentryAIOptions;

    public SentryChatClient(IChatClient client, Action<SentryAIOptions>? configure = null) : base(client)
    {
        _sentryAIOptions = new SentryAIOptions();
        configure?.Invoke(_sentryAIOptions);

        if (_sentryAIOptions.InitializeSdk && !SentrySdk.IsEnabled)
        {
            SentrySdk.Init(_sentryAIOptions);
        }

        _hub = HubAdapter.Instance;
    }

    /// <inheritdoc cref="IChatClient"/>
    public override async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = new())
    {
        var chatMessages = messages as ChatMessage[] ?? messages.ToArray();
        var keyMessage = chatMessages[0];
        var outerSpan = CreateOrGetRootSpan(keyMessage, options);
        var innerSpan = CreateChatSpan(outerSpan, options);
        var spanDict = GetMessageToSpanDict(options);
        SetMessageToSpanDict(keyMessage, outerSpan, options);

        try
        {
            SentryAISpanEnricher.EnrichWithRequest(innerSpan, chatMessages, options, _sentryAIOptions);

            var response = await base.GetResponseAsync(chatMessages, options, cancellationToken).ConfigureAwait(false);

            SentryAISpanEnricher.EnrichWithResponse(innerSpan, response, _sentryAIOptions);
            innerSpan.Finish(SpanStatus.Ok);

            // Only finish the outerSpan if this response's finish reason is stop (not tool calls).
            // This allows the outerSpan to persist throughout multiple `GetResponseAsync` calls
            // happening before and after tool calls
            if (response.FinishReason == ChatFinishReason.Stop)
            {
                outerSpan.Finish(SpanStatus.Ok);
                spanDict.Remove(keyMessage, out _);
            }
            else if (response.FinishReason == ChatFinishReason.ToolCalls)
            {
                WrapFunctionCallsInResponse(response, keyMessage);
            }

            return response;
        }
        catch (Exception ex)
        {
            innerSpan.Finish(ex);
            outerSpan.Finish(ex);
            _hub.CaptureException(ex);
            spanDict.Remove(keyMessage, out _);
            throw;
        }
    }

    /// <inheritdoc cref="IChatClient"/>
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = new())
    {
        var chatMessages = messages as ChatMessage[] ?? messages.ToArray();
        var keyMessage = chatMessages[0];
        var outerSpan = CreateOrGetRootSpan(keyMessage, options);
        var innerSpan = CreateChatSpan(outerSpan, options);
        var spanDict = GetMessageToSpanDict(options);
        SetMessageToSpanDict(keyMessage, outerSpan, options);

        ChatResponseUpdate? current = null;
        var responses = new List<ChatResponseUpdate>();
        var enumerator = base
            .GetStreamingResponseAsync(chatMessages, options, cancellationToken)
            .GetAsyncEnumerator(cancellationToken);
        SentryAISpanEnricher.EnrichWithRequest(innerSpan, chatMessages, options, _sentryAIOptions);

        while (true)
        {
            try
            {
                var hasNext = await enumerator.MoveNextAsync().ConfigureAwait(false);

                if (!hasNext)
                {
                    SentryAISpanEnricher.EnrichWithStreamingResponse(innerSpan, responses, _sentryAIOptions);
                    innerSpan.Finish(SpanStatus.Ok);

                    // Only if currentFinishReason is to stop, then we finish the RootSpan and set it to null.
                    // This allows the RootSpan to persist throughout multiple `GetStreamingResponseAsync` calls
                    // happening before and after tool calls
                    var shouldFinishRootSpan = current?.FinishReason == ChatFinishReason.Stop;
                    if (shouldFinishRootSpan)
                    {
                        outerSpan.Finish(SpanStatus.Ok);
                        spanDict.Remove(keyMessage, out _);
                    }
                    else if (current?.FinishReason == ChatFinishReason.ToolCalls)
                    {
                        WrapFunctionCallsInResponse(current, keyMessage);
                    }

                    yield break;
                }

                current = enumerator.Current;
                responses.Add(enumerator.Current);
            }
            catch (Exception ex)
            {
                innerSpan.Finish(ex);
                outerSpan.Finish(ex);
                _hub.CaptureException(ex);
                spanDict.Remove(keyMessage, out _);
                throw;
            }

            yield return current;
        }
    }

    /// <summary>
    /// We create an entry in _spans concurrent dictionary to keep track of
    /// what root span to use in consequent calls of <see cref="GetResponseAsync"/> or <see cref="GetStreamingResponseAsync"/>
    /// </summary>
    /// <param name="message"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    private ISpan CreateOrGetRootSpan(ChatMessage message, ChatOptions? options)
    {
        var spanDict = GetMessageToSpanDict(options);
        if (!spanDict.TryGetValue(message, out var rootSpan))
        {
            var invokeSpanName = $"invoke_agent {InnerClient.GetType().Name}";
            const string invokeOperation = "gen_ai.invoke_agent";
            rootSpan = _hub.StartSpan(invokeOperation, invokeSpanName);
            rootSpan.SetData("gen_ai.agent.name", $"{InnerClient.GetType().Name}");
        }

        return rootSpan;
    }

    private static ISpan CreateChatSpan(ISpan outerSpan, ChatOptions? options)
    {
        const string chatOperation = "gen_ai.chat";
        var chatSpanName = options is null || string.IsNullOrEmpty(options.ModelId)
            ? "chat unknown model"
            : $"chat {options.ModelId}";
        return outerSpan.StartChild(chatOperation, chatSpanName);
    }

    internal static ConcurrentDictionary<ChatMessage, ISpan> GetMessageToSpanDict(ChatOptions? options = null)
    {
        if (options?.AdditionalProperties?.TryGetValue<ConcurrentDictionary<ChatMessage, ISpan>>(
                SentryAIConstants.OptionsAdditionalAttributeAgentSpanName, out var agentSpanDict) == true)
        {
            return agentSpanDict;
        }

        // If we couldn't find the dictionary, we just initiate it now
        agentSpanDict = new ConcurrentDictionary<ChatMessage, ISpan>();
        if (options == null)
        {
            return agentSpanDict;
        }

        options.AdditionalProperties = new AdditionalPropertiesDictionary();
        options.AdditionalProperties.TryAdd(SentryAIConstants.OptionsAdditionalAttributeAgentSpanName, agentSpanDict);
        return agentSpanDict;
    }

    private static void SetMessageToSpanDict(ChatMessage message, ISpan agentSpan, ChatOptions? options)
    {
        ConcurrentDictionary<ChatMessage, ISpan>? agentSpanDict = null;
        if (options == null ||
            options.AdditionalProperties?.TryGetValue(SentryAIConstants.OptionsAdditionalAttributeAgentSpanName,
                out agentSpanDict) == false)
        {
            return;
        }

        agentSpanDict?.TryAdd(message, agentSpan);
    }

    private static void WrapFunctionCallsInResponse(ChatResponse response, ChatMessage keyMessage)
    {
        foreach (var message in response.Messages)
        {
            foreach (var content in message.Contents)
            {
                if (content is FunctionCallContent functionCall)
                {
                    (functionCall.Arguments ??= new Dictionary<string, object?>()).Add(
                        SentryAIConstants.KeyMessageFunctionArgumentDictKey, keyMessage);
                }
            }
        }
    }

    private static void WrapFunctionCallsInResponse(ChatResponseUpdate response, ChatMessage keyMessage)
    {
        foreach (var content in response.Contents)
        {
            if (content is FunctionCallContent functionCall)
            {
                (functionCall.Arguments ??= new Dictionary<string, object?>()).Add(
                    SentryAIConstants.KeyMessageFunctionArgumentDictKey, keyMessage);
            }
        }
    }
}
