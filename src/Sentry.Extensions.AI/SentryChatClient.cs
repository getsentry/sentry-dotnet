using Microsoft.Extensions.AI;

namespace Sentry.Extensions.AI;

internal sealed class SentryChatClient(
    IChatClient innerClient,
    IHub hub,
    string? agentName,
    string? system)
    : DelegatingChatClient(innerClient)
{
    public override async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = new())
    {
        const string operation = "gen_ai.chat";
        var spanName = agentName is { Length: > 0 } ? $"chat {agentName}" : "chat";
        var initialSpan = hub.GetSpan()?.StartChild(operation, spanName) ?? hub.StartTransaction(spanName, operation);

        try
        {
            var chatMessages = messages as ChatMessage[] ?? messages.ToArray();
            SentryAISpanEnricher.EnrichWithRequest(initialSpan, chatMessages, options, agentName, system);

            var response = await base.GetResponseAsync(chatMessages, options, cancellationToken).ConfigureAwait(false);

            SentryAISpanEnricher.EnrichWithResponse(initialSpan, response);
            initialSpan.Finish(SpanStatus.Ok);
            return response;
        }
        catch (Exception ex)
        {
            initialSpan.Finish(ex);
            hub.CaptureException(ex);
            throw;
        }
    }
}
