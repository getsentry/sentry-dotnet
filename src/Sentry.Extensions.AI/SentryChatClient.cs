using Microsoft.Extensions.AI;
using Sentry.Extensibility;

namespace Sentry.Extensions.AI;

internal sealed class SentryChatClient : DelegatingChatClient
{
    private readonly HubAdapter _hub;
    private readonly SentryAIOptions _sentryAIOptions;
    internal static ISpan? RootSpan;

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
        var outerSpan = EnsureRootSpanExists();
        var innerSpan = CreateChatSpan(options, outerSpan);

        try
        {
            var chatMessages = messages as ChatMessage[] ?? messages.ToArray();
            SentryAISpanEnricher.EnrichWithRequest(innerSpan, chatMessages, options, _sentryAIOptions);

            var response = await base.GetResponseAsync(chatMessages, options, cancellationToken).ConfigureAwait(false);

            SentryAISpanEnricher.EnrichWithResponse(innerSpan, response, _sentryAIOptions);
            innerSpan.Finish(SpanStatus.Ok);

            if (!ContainsFunctionCalls(response))
            {
                outerSpan.Finish(SpanStatus.Ok);
                RootSpan = null;
            }

            return response;
        }
        catch (Exception ex)
        {
            innerSpan.Finish(ex);
            outerSpan.Finish(ex);
            _hub.CaptureException(ex);
            RootSpan = null;
            throw;
        }
    }

    /// <inheritdoc cref="IChatClient"/>
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = new())
    {
        var outerSpan = EnsureRootSpanExists();
        var innerSpan = CreateChatSpan(options, outerSpan);

        var responses = new List<ChatResponseUpdate>();
        var chatMessages = messages as ChatMessage[] ?? messages.ToArray();
        var enumerator = base
            .GetStreamingResponseAsync(chatMessages, options, cancellationToken)
            .GetAsyncEnumerator(cancellationToken);

        while (true)
        {
            ChatResponseUpdate? current;

            try
            {
                SentryAISpanEnricher.EnrichWithRequest(innerSpan, chatMessages, options, _sentryAIOptions);
                var hasNext = await enumerator.MoveNextAsync().ConfigureAwait(false);
                if (!hasNext)
                {
                    SentryAISpanEnricher.EnrichWithStreamingResponse(innerSpan, responses, _sentryAIOptions);
                    innerSpan.Finish(SpanStatus.Ok);
                    outerSpan.Finish(SpanStatus.Ok);

                    if (!ContainsFunctionCalls(responses))
                    {
                        RootSpan = null;
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
                RootSpan = null;
                throw;
            }

            yield return current;
        }
    }

    private ISpan EnsureRootSpanExists()
    {
        if (RootSpan == null)
        {
            var invokeSpanName = $"invoke_agent {InnerClient.GetType().Name}";
            const string invokeOperation = "gen_ai.invoke_agent";
            RootSpan = _hub.StartSpan(invokeOperation, invokeSpanName);
            RootSpan.SetData("gen_ai.agent.name", $"{InnerClient.GetType().Name}");
        }

        return RootSpan;
    }

    private static ISpan CreateChatSpan(ChatOptions? options, ISpan outerSpan)
    {
        const string chatOperation = "gen_ai.chat";
        var chatSpanName = options is null || string.IsNullOrEmpty(options.ModelId)
            ? "chat unknown model"
            : $"chat {options.ModelId}";
        return outerSpan.StartChild(chatOperation, chatSpanName);
    }

    private static bool ContainsFunctionCalls(ChatResponse response) =>
        response.Messages.Any(m => m.Contents?.OfType<FunctionCallContent>().Any() ?? false)
        || response.FinishReason == ChatFinishReason.ToolCalls;

    private static bool ContainsFunctionCalls(List<ChatResponseUpdate> responses) =>
        responses.Any(m => m.Contents?.OfType<FunctionCallContent>().Any() ?? false)
        || responses.Any(m => m.FinishReason == ChatFinishReason.ToolCalls);
}
