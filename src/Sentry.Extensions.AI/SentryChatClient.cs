using Microsoft.Extensions.AI;

namespace Sentry.Extensions.AI;

internal sealed class SentryChatClient(
    IChatClient innerClient,
    IHub hub)
    : DelegatingChatClient(innerClient)
{
    public override async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = new())
    {
        const string operation = "gen_ai.chat";
        var spanName = InnerClient.GetType().Name;
        var initialSpan = hub.GetSpan()?.StartChild(operation, spanName) ?? hub.StartTransaction(spanName, operation);

        try
        {
            var chatMessages = messages as ChatMessage[] ?? messages.ToArray();
            SentryAISpanEnricher.EnrichWithRequest(initialSpan, chatMessages, options);

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
