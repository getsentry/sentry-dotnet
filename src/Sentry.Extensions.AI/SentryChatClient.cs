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

        if (_sentryAIOptions.InitializeSdk)
        {
            if (!SentrySdk.IsEnabled || _sentryAIOptions.Dsn is not null)
            {
                // Initialize Sentry with our options/DSN
                var hub = SentrySdk.InitHub(_sentryAIOptions);
                SentrySdk.UseHub(hub);
            }
        }

        _hub = HubAdapter.Instance;
    }

    /// <inheritdoc cref="IChatClient"/>
    public override async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = new())
    {
        var invokeSpanName = $"invoke_agent {InnerClient.GetType().Name}";
        const string invokeOperation = "gen_ai.invoke_agent";
        var outerSpan = StartSpanOrTransaction(invokeOperation, invokeSpanName);

        const string chatOperation = "gen_ai.chat";
        var chatSpanName = options is null || string.IsNullOrEmpty(options.ModelId) ? "chat unknown model" : $"chat {options.ModelId}";
        var initialSpan = outerSpan.StartChild(chatOperation, chatSpanName);

        try
        {
            var chatMessages = messages as ChatMessage[] ?? messages.ToArray();
            SentryAISpanEnricher.EnrichWithRequest(initialSpan, chatMessages, options, _sentryAIOptions);

            var response = await base.GetResponseAsync(chatMessages, options, cancellationToken).ConfigureAwait(false);

            SentryAISpanEnricher.EnrichWithResponse(initialSpan, response, _sentryAIOptions);
            initialSpan.Finish(SpanStatus.Ok);
            outerSpan.Finish(SpanStatus.Ok);
            return response;
        }
        catch (Exception ex)
        {
            initialSpan.Finish(ex);
            outerSpan.Finish(ex);
            _hub.CaptureException(ex);
            throw;
        }
    }

    /// <inheritdoc cref="IChatClient"/>
    public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = new())
    {
        var invokeSpanName = $"invoke_agent {InnerClient.GetType().Name}";
        const string invokeOperation = "gen_ai.invoke_agent";
        var outerSpan = StartSpanOrTransaction(invokeOperation, invokeSpanName);

        const string chatOperation = "gen_ai.chat";
        var chatSpanName = options is null || string.IsNullOrEmpty(options.ModelId) ? "chat unknown model" : $"chat {options.ModelId}";
        var initialSpan = outerSpan.StartChild(chatOperation, chatSpanName);

        try
        {
            return InstrumentStreamingResponseAsync(messages, options, outerSpan, initialSpan, cancellationToken);
        }
        catch (Exception ex)
        {
            initialSpan.Finish(ex);
            outerSpan.Finish(ex);
            _hub.CaptureException(ex);
            throw;
        }
    }

    private async IAsyncEnumerable<ChatResponseUpdate> InstrumentStreamingResponseAsync(IEnumerable<ChatMessage> messages,
        ChatOptions? options,
        ISpan outerSpan,
        ISpan span,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var chatMessages = messages as ChatMessage[] ?? messages.ToArray();
        SentryAISpanEnricher.EnrichWithRequest(span, chatMessages, options, _sentryAIOptions);

        var responses = new List<ChatResponseUpdate>();
        var originalStream = base.GetStreamingResponseAsync(chatMessages, options, cancellationToken);

        await foreach (var chunk in originalStream.ConfigureAwait(false))
        {
            responses.Add(chunk);

            yield return chunk;
        }

        SentryAISpanEnricher.EnrichWithStreamingResponse(span, responses, _sentryAIOptions);
        span.Finish(SpanStatus.Ok);
        outerSpan.Finish(SpanStatus.Ok);
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
}
