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
        var outerSpan = TryGetRootSpan(options);
        var innerSpan = CreateChatSpan(outerSpan, options);

        try
        {
            SentryAISpanEnricher.EnrichWithRequest(innerSpan, chatMessages, options, _sentryAIOptions);

            var response = await base.GetResponseAsync(chatMessages, options, cancellationToken).ConfigureAwait(false);

            SentryAISpanEnricher.EnrichWithResponse(innerSpan, response, _sentryAIOptions);
            innerSpan.Finish(SpanStatus.Ok);

            return response;
        }
        catch (Exception ex)
        {
            innerSpan.Finish(ex);
            _hub.CaptureException(ex);
            throw;
        }
        finally
        {
            // if options was null, we need to finish root span immediately because no tool calls will be made
            // therefore no consequent GetResponseAsync calls
            if (options == null)
            {
                outerSpan?.Finish();
            }
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
        var outerSpan = TryGetRootSpan(options);
        var innerSpan = CreateChatSpan(outerSpan, options);

        var hasNext = true;
        var responses = new List<ChatResponseUpdate>();
        var enumerator = base
            .GetStreamingResponseAsync(chatMessages, options, cancellationToken)
            .GetAsyncEnumerator(cancellationToken);
        SentryAISpanEnricher.EnrichWithRequest(innerSpan, chatMessages, options, _sentryAIOptions);

        while (true)
        {
            ChatResponseUpdate? current;
            try
            {
                hasNext = await enumerator.MoveNextAsync().ConfigureAwait(false);

                if (!hasNext)
                {
                    SentryAISpanEnricher.EnrichWithStreamingResponses(innerSpan, responses, _sentryAIOptions);
                    innerSpan.Finish(SpanStatus.Ok);

                    yield break;
                }

                current = enumerator.Current;
                responses.Add(enumerator.Current);
            }
            catch (Exception ex)
            {
                innerSpan.Finish(ex);
                _hub.CaptureException(ex);
                throw;
            }
            finally
            {
                // if options was null, and we don't have next text, we need to finish root span immediately
                if (options == null && !hasNext)
                {
                    outerSpan?.Finish(SpanStatus.Ok);
                }
            }

            yield return current;
        }
    }

    /// <inheritdoc/>
    public override object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceType == typeof(ActivitySource)
            ? SentryAIActivitySource.Instance
            : base.GetService(serviceType, serviceKey);

    private ISpan? TryGetRootSpan(ChatOptions? options)
    {
        // if options is null, we are not doing tool calls, so it's safe to just return an invoke_agent span
        // straight from the hub
        if (options == null)
        {
            return _hub.StartSpan(SentryAIConstants.SpanAttributes.InvokeAgentOperation,
                SentryAIConstants.SpanAttributes.InvokeAgentDescription);
        }

        // If FunctionInvokingChatClient wraps SentryChatClient, we should be able to get the agent span from the current activity
        // The activity we attached the span to may be an ancestor of the current activity, we must search the parents for the span
        var activeSpan = SentryAIUtil.GetActivitySpan();
        return activeSpan;
    }

    private ISpan CreateChatSpan(ISpan? outerSpan, ChatOptions? options)
    {
        var chatSpanName = options is null || string.IsNullOrEmpty(options.ModelId)
            ? "chat unknown model"
            : $"chat {options.ModelId}";
        return outerSpan is not null
            ? outerSpan.StartChild(SentryAIConstants.SpanAttributes.ChatOperation, chatSpanName)
            : _hub.StartSpan(SentryAIConstants.SpanAttributes.ChatOperation, chatSpanName);
    }
}
