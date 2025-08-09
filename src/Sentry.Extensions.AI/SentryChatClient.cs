using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Sentry;
using System.Runtime.CompilerServices;

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
            transaction.Finish(ex);
            _hub.CaptureException(ex);
            throw;
        }
    }

    public IAsyncEnumerable<StreamingChatCompletionUpdate> CompleteStreamingAsync(IList<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        return new SentryStreamingChatEnumerable(_innerClient.CompleteStreamingAsync(messages, options, cancellationToken), _hub, _agentName, _model, _system);
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

internal sealed class SentryStreamingChatEnumerable : IAsyncEnumerable<StreamingChatCompletionUpdate>
{
    private readonly IAsyncEnumerable<StreamingChatCompletionUpdate> _innerEnumerable;
    private readonly IHub _hub;
    private readonly string? _agentName;
    private readonly string? _model;
    private readonly string? _system;

    public SentryStreamingChatEnumerable(
        IAsyncEnumerable<StreamingChatCompletionUpdate> innerEnumerable,
        IHub hub,
        string? agentName,
        string? model,
        string? system)
    {
        _innerEnumerable = innerEnumerable;
        _hub = hub;
        _agentName = agentName;
        _model = model;
        _system = system;
    }

    public IAsyncEnumerator<StreamingChatCompletionUpdate> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new SentryStreamingChatEnumerator(_innerEnumerable.GetAsyncEnumerator(cancellationToken), _hub, _agentName, _model, _system);
    }
}

internal sealed class SentryStreamingChatEnumerator : IAsyncEnumerator<StreamingChatCompletionUpdate>
{
    private readonly IAsyncEnumerator<StreamingChatCompletionUpdate> _innerEnumerator;
    private readonly ISpan _transaction;
    private readonly IHub _hub;
    private bool _finished;

    public SentryStreamingChatEnumerator(
        IAsyncEnumerator<StreamingChatCompletionUpdate> innerEnumerator,
        IHub hub,
        string? agentName,
        string? model,
        string? system)
    {
        _innerEnumerator = innerEnumerator;
        _hub = hub;

        // Create the span/transaction
        var operation = "gen_ai.invoke_agent";
        var spanName = agentName is { Length: > 0 } ? $"invoke_agent {agentName}" : "invoke_agent";
        _transaction = hub.GetSpan()?.StartChild(operation, spanName) ?? hub.StartTransaction(spanName, operation);

        // Set the same tags as CompleteAsync
        if (system is { Length: > 0 })
        {
            _transaction.SetTag("gen_ai.system", system);
        }

        if (model is { Length: > 0 })
        {
            _transaction.SetTag("gen_ai.request.model", model);
        }

        _transaction.SetTag("gen_ai.operation.name", "invoke_agent");
        if (agentName is { Length: > 0 })
        {
            _transaction.SetTag("gen_ai.agent.name", agentName);
        }

        // Add streaming-specific tag
        _transaction.SetTag("gen_ai.streaming", "true");
    }

    public StreamingChatCompletionUpdate Current => _innerEnumerator.Current;

    public async ValueTask<bool> MoveNextAsync()
    {
        try
        {
            var hasNext = await _innerEnumerator.MoveNextAsync().ConfigureAwait(false);
            
            if (!hasNext && !_finished)
            {
                _transaction.Finish(SpanStatus.Ok);
                _finished = true;
            }
            
            return hasNext;
        }
        catch (Exception ex)
        {
            if (!_finished)
            {
                _transaction.Finish(ex);
                _hub.CaptureException(ex);
                _finished = true;
            }
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await _innerEnumerator.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            if (!_finished)
            {
                _transaction.Finish(SpanStatus.Ok);
                _finished = true;
            }
        }
    }
}


