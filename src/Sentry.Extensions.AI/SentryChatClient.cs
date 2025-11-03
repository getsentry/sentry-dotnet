using Microsoft.Extensions.AI;
using Sentry.Extensibility;
using Sentry.Internal;

namespace Sentry.Extensions.AI;

internal sealed class SentryChatClient : DelegatingChatClient
{
    private readonly HubAdapter _hub = HubAdapter.Instance;
    private readonly SentryAIOptions _sentryAIOptions;

    public SentryChatClient(IChatClient client, Action<SentryAIOptions>? configure = null) : base(client)
    {
        _sentryAIOptions = new SentryAIOptions();
        configure?.Invoke(_sentryAIOptions);

        // If user requested to initialize the SDK, and SDK is not enabled already, then use the options to init Sentry
        if (_sentryAIOptions.InitializeSdk && !SentrySdk.IsEnabled)
        {
            SentrySdk.Init(_sentryAIOptions);
        }
    }

    /// <inheritdoc cref="IChatClient"/>
    public override async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = new())
    {
        // Convert to array to avoid multiple enumeration
        var chatMessages = messages as ChatMessage[] ?? messages.ToArray();
        var agentSpan = TryGetAgentSpan(options);
        var chatSpan = CreateChatSpan(agentSpan, options);

        try
        {
            SentryAISpanEnricher.EnrichWithRequest(chatSpan, chatMessages, options, _sentryAIOptions,
                SentryAIConstants.SpanOperations.Chat);
            SentryAISpanEnricher.EnrichWithRequest(agentSpan, chatMessages, options, _sentryAIOptions,
                SentryAIConstants.SpanOperations.InvokeAgent);

            var response = await base.GetResponseAsync(chatMessages, options, cancellationToken).ConfigureAwait(false);

            SentryAISpanEnricher.EnrichWithResponse(chatSpan, response, _sentryAIOptions);
            AfterResponseCleanup(chatSpan, agentSpan, options);

            return response;
        }
        catch (Exception ex)
        {
            AfterResponseCleanup(chatSpan, agentSpan, options, ex);
            throw;
        }
    }

    /// <inheritdoc cref="IChatClient"/>
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = new())
    {
        // Convert to array to avoid multiple enumeration
        var chatMessages = messages as ChatMessage[] ?? messages.ToArray();
        var agentSpan = TryGetAgentSpan(options);
        var chatSpan = CreateChatSpan(agentSpan, options);

        var responses = new List<ChatResponseUpdate>();

        // Incorrect Roslyn analyzer error when doing await using on IAsyncDisposable
        // https://github.com/dotnet/roslyn-analyzers/issues/5712
#pragma warning disable CA2007
        await using var enumerator = base
            .GetStreamingResponseAsync(chatMessages, options, cancellationToken)
            .ConfigureAwait(false)
            .GetAsyncEnumerator();
#pragma warning restore CA2007
        SentryAISpanEnricher.EnrichWithRequest(chatSpan, chatMessages, options, _sentryAIOptions,
            SentryAIConstants.SpanOperations.Chat);
        SentryAISpanEnricher.EnrichWithRequest(agentSpan, chatMessages, options, _sentryAIOptions,
            SentryAIConstants.SpanOperations.InvokeAgent);

        while (true)
        {
            ChatResponseUpdate? current;
            try
            {
                var hasNext = await enumerator.MoveNextAsync();

                if (!hasNext)
                {
                    SentryAISpanEnricher.EnrichWithStreamingResponses(chatSpan, responses, _sentryAIOptions);
                    AfterResponseCleanup(chatSpan, agentSpan, options);

                    yield break;
                }

                current = enumerator.Current;
                responses.Add(current);
            }
            catch (Exception ex)
            {
                AfterResponseCleanup(chatSpan, agentSpan, options, ex);
                throw;
            }

            yield return current;
        }
    }

    /// <inheritdoc/>
    public override object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceType == typeof(ActivitySource)
            ? SentryAIActivitySource.Instance
            : base.GetService(serviceType, serviceKey);

    private void AfterResponseCleanup(ISpan chatSpan, ISpan agentSpan, ChatOptions? options,
        Exception? exception = null)
    {
        // If there was an exception, we finish all spans and return
        if (exception != null)
        {
            chatSpan.Finish(exception);
            agentSpan.Finish(exception);
            _hub.CaptureException(exception);
            return;
        }

        chatSpan.Finish(SpanStatus.Ok);
        // If we didn't have any tools available, we can just finish outer invoke_agent span.
        if (options?.Tools == null)
        {
            agentSpan.Finish(SpanStatus.Ok);
        }
    }

    private ISpan TryGetAgentSpan(ChatOptions? options)
    {
        // if tools list is null, we are not doing tool calls, so it's safe to just return an invoke_agent span
        // straight from the hub
        if (options?.Tools == null)
        {
            return _hub.StartSpan(SentryAIConstants.SpanAttributes.InvokeAgentOperation,
                SentryAIConstants.SpanAttributes.InvokeAgentDescription);
        }

        // If FunctionInvokingChatClient(FICC) wraps SentryChatClient, we should be able to get the agent span from the current activity
        // The activity we attached the span to may be an ancestor of the current activity, we must search the parents for the span
        var activeSpan = SentryAIUtil.GetActivitySpan();

        // If we couldn't find the Activity, then FICC is not wrapping SentryChatClient. Return a new span from the hub
        return activeSpan ?? _hub.StartSpan(SentryAIConstants.SpanAttributes.InvokeAgentOperation,
            SentryAIConstants.SpanAttributes.InvokeAgentDescription);
    }

    private ISpan CreateChatSpan(ISpan? agentSpan, ChatOptions? options)
    {
        var chatSpanName = options is null || string.IsNullOrEmpty(options.ModelId)
            ? "chat unknown model"
            : $"chat {options.ModelId}";
        return agentSpan is not null
            ? agentSpan.StartChild(SentryAIConstants.SpanAttributes.ChatOperation, chatSpanName)
            : _hub.StartSpan(SentryAIConstants.SpanAttributes.ChatOperation, chatSpanName);
    }
}
