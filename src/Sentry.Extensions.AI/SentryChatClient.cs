using Microsoft.Extensions.AI;
using Sentry.Extensibility;

namespace Sentry.Extensions.AI;

internal sealed class SentryChatClient : DelegatingChatClient
{
    private readonly HubAdapter _hub;
    private readonly SentryAIOptions _sentryAIOptions;
    private static ISpan? RootSpan;

    public SentryChatClient(IChatClient client, Action<SentryAIOptions>? configure = null) : base(client)
    {
        _sentryAIOptions = new SentryAIOptions();
        configure?.Invoke(_sentryAIOptions);

        if (_sentryAIOptions.InitializeSdk && !SentrySdk.IsEnabled)
        {
            var hub = SentrySdk.InitHub(_sentryAIOptions);
            SentrySdk.UseHub(hub);
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

    /// <summary>
    /// Starts a span or transaction based on whether there's an active transaction context.
    /// </summary>
    /// <param name="operation">The operation name</param>
    /// <param name="description">The span/transaction description</param>
    /// <returns>A child span of an existing transaction if available, else a new transaction</returns>
    private ISpan StartSpanOrTransaction(string operation, string description)
    {
        var currentSpan = _hub.GetSpan();

        if (currentSpan?.GetTransaction() != null)
        {
            return currentSpan.StartChild(operation, description);
        }

        var newTransaction = _hub.StartTransaction(description, operation);
        _hub.ConfigureScope(scope => scope.Transaction = newTransaction);
        return newTransaction;
    }

    private static bool ContainsFunctionCalls(ChatResponse response) =>
        response.Messages.Any(m => m.Contents?.OfType<FunctionCallContent>().Any() ?? false)
        || response.FinishReason == ChatFinishReason.ToolCalls;

    private static bool ContainsFunctionCalls(List<ChatResponseUpdate> responses) =>
        responses.Any(m => m.Contents?.OfType<FunctionCallContent>().Any() ?? false)
        || responses.Any(m => m.FinishReason == ChatFinishReason.ToolCalls);

    private ISpan EnsureRootSpanExists()
    {
        var invokeSpanName = $"invoke_agent {InnerClient.GetType().Name}";
        const string invokeOperation = "gen_ai.invoke_agent";
        RootSpan ??= StartSpanOrTransaction(invokeOperation, invokeSpanName);
        // In ME.AI, there's not really an agent name. In other SDKs we set this, so we should do so here
        RootSpan.SetData("gen_ai.agent.name", $"{InnerClient.GetType().Name}");
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
}
