using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Sentry;

namespace Sentry.Extensions.AI;

internal sealed class SentryChatClient : IChatClient
{
    private readonly IChatClient _innerClient;
    private readonly IHub _hub;
    private readonly string? _agentName;
    private readonly string? _model;
    private readonly string? _system;

    public SentryChatClient(IChatClient innerClient, IHub hub, string? agentName = null, string? model = null, string? system = null)
    {
        _innerClient = innerClient;
        _hub = hub;
        _agentName = agentName;
        _model = model;
        _system = system;
    }

    public ChatClientMetadata Metadata => _innerClient.Metadata;

    public async Task<ChatCompletion> CompleteAsync(IList<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var operation = "gen_ai.invoke_agent";
        var spanName = _agentName is { Length: > 0 } ? $"invoke_agent {_agentName}" : "invoke_agent";
        var transaction = _hub.GetSpan()?.StartChild(operation, spanName) ?? _hub.StartTransaction(spanName, operation);

        if (_system is { Length: > 0 })
        {
            transaction.SetTag("gen_ai.system", _system);
        }

        if (_model is { Length: > 0 })
        {
            transaction.SetTag("gen_ai.request.model", _model);
        }

        transaction.SetTag("gen_ai.operation.name", "invoke_agent");
        if (_agentName is { Length: > 0 })
        {
            transaction.SetTag("gen_ai.agent.name", _agentName);
        }

        try
        {
            var response = await _innerClient.CompleteAsync(messages, options, cancellationToken).ConfigureAwait(false);
            transaction.Finish(SpanStatus.Ok);
            return response;
        }
        catch (Exception ex)
        {
            transaction.Finish(SpanStatus.InternalError);
            _hub.CaptureException(ex);
            throw;
        }
    }

    public IAsyncEnumerable<StreamingChatCompletionUpdate> CompleteStreamingAsync(IList<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        // For streaming, we wrap the enumerable but don't create spans for each chunk
        return _innerClient.CompleteStreamingAsync(messages, options, cancellationToken);
    }

    public TService? GetService<TService>(object? key = null) where TService : class
    {
        return _innerClient.GetService<TService>(key);
    }

    public void Dispose()
    {
        _innerClient?.Dispose();
    }
}


