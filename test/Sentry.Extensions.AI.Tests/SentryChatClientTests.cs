#nullable enable
using Microsoft.Extensions.AI;

namespace Sentry.Extensions.AI.Tests;

public class SentryChatClientTests
{
    private class Fixture
    {
        private SentryOptions Options { get; }
        public IHub Hub { get; }
        public ActivitySource Source { get; } = SentryAIActivitySource.CreateSource();
        public IChatClient InnerClient = Substitute.For<IChatClient>();

        public Fixture()
        {
            Options = new SentryOptions
            {
                Dsn = ValidDsn,
                TracesSampleRate = 1.0,
            };

            SentrySdk.Init(Options);
            Hub = SentrySdk.CurrentHub;
        }

        public SentryChatClient GetSut() => new SentryChatClient(Source, InnerClient);
    }

    private readonly Fixture _fixture = new();

    [Fact]
    public async Task CompleteAsync_CallsInnerClient_AndSetsData()
    {
        // Arrange
        var transaction = _fixture.Hub.StartTransaction("test-nonstreaming", "test");
        _fixture.Hub.ConfigureScope(scope => scope.Transaction = transaction);
        SentrySdk.ConfigureScope(scope => scope.Transaction = transaction);

        var message = new ChatMessage(ChatRole.Assistant, "ok");
        var chatResponse = new ChatResponse(message);

        _fixture.InnerClient.GetResponseAsync(Arg.Any<IList<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(chatResponse));
        var sentryChatClient = _fixture.GetSut();

        // Act
        var res = await sentryChatClient.GetResponseAsync([new ChatMessage(ChatRole.User, "hi")]);

        // Assert
        Assert.Equal([message], res.Messages);
        await _fixture.InnerClient.Received(1).GetResponseAsync(Arg.Any<IList<ChatMessage>>(), Arg.Any<ChatOptions>(),
            Arg.Any<CancellationToken>());

        var chatSpan = transaction.Spans.FirstOrDefault(s => s.Operation == SentryAIConstants.SpanAttributes.ChatOperation);
        var agentSpan = transaction.Spans.FirstOrDefault(s => s.Operation == SentryAIConstants.SpanAttributes.InvokeAgentOperation);

        Assert.NotNull(chatSpan);
        Assert.True(chatSpan.IsFinished);
        Assert.Equal(SpanStatus.Ok, chatSpan.Status);
        Assert.Equal(SentryAIConstants.SpanOperations.Chat, chatSpan.Data[SentryAIConstants.SpanAttributes.OperationName]);
        Assert.Equal("ok", chatSpan.Data[SentryAIConstants.SpanAttributes.ResponseText]);

        Assert.NotNull(agentSpan);
        Assert.True(agentSpan.IsFinished);
        Assert.Equal(SpanStatus.Ok, agentSpan.Status);
        Assert.Equal(SentryAIConstants.SpanOperations.InvokeAgent, agentSpan.Data[SentryAIConstants.SpanAttributes.OperationName]);
    }

    [Fact]
    public async Task CompleteAsync_HandlesErrors_AndFinishesSpanWithException()
    {
        // Arrange
        var transaction = _fixture.Hub.StartTransaction("test-nonstreaming", "test");
        _fixture.Hub.ConfigureScope(scope => scope.Transaction = transaction);
        SentrySdk.ConfigureScope(scope => scope.Transaction = transaction);

        var sentryChatClient = _fixture.GetSut();
        var expectedException = new InvalidOperationException("Streaming failed");

        _fixture.InnerClient.GetResponseAsync(Arg.Any<IList<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>())
            .Throws(expectedException);

        // Act
        var res = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await sentryChatClient.GetResponseAsync([new ChatMessage(ChatRole.User, "hi")]));

        // Assert
        Assert.Equal(expectedException.Message, res.Message);
        var spans = transaction.Spans;
        var chatSpan = spans.FirstOrDefault(s => s.Operation == SentryAIConstants.SpanAttributes.ChatOperation);
        var agentSpan = spans.FirstOrDefault(s => s.Operation == SentryAIConstants.SpanAttributes.InvokeAgentOperation);

        Assert.NotNull(chatSpan);
        Assert.Equal(SpanStatus.InternalError, chatSpan.Status);
        Assert.True(chatSpan.IsFinished);
        Assert.Equal(SentryAIConstants.SpanOperations.Chat, chatSpan.Data[SentryAIConstants.SpanAttributes.OperationName]);

        Assert.NotNull(agentSpan);
        Assert.True(agentSpan.IsFinished);
        Assert.Equal(SpanStatus.InternalError, agentSpan.Status);
        Assert.Equal(SentryAIConstants.SpanOperations.InvokeAgent, agentSpan.Data[SentryAIConstants.SpanAttributes.OperationName]);
    }

    [Fact]
    public async Task CompleteStreamingAsync_CallsInnerClient_AndSetsSpanData()
    {
        // Arrange - Use Fixture Hub to start transaction
        var transaction = _fixture.Hub.StartTransaction("test-streaming", "test");
        _fixture.Hub.ConfigureScope(scope => scope.Transaction = transaction);
        SentrySdk.ConfigureScope(scope => scope.Transaction = transaction);

        _fixture.InnerClient.GetStreamingResponseAsync(Arg.Any<IList<ChatMessage>>(), Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(CreateTestStreamingUpdatesAsync());
        var sentryChatClient = _fixture.GetSut();
        var results = new List<ChatResponseUpdate>();

        // Act
        await foreach (var update in sentryChatClient.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "hi")]))
        {
            results.Add(update);
        }

        // Assert
        _fixture.InnerClient.Received(1).GetStreamingResponseAsync(Arg.Any<IList<ChatMessage>>(), Arg.Any<ChatOptions>(),
            Arg.Any<CancellationToken>());
        Assert.Equal(2, results.Count);
        Assert.Equal("Hello", results[0].Text);
        Assert.Equal(" World!", results[1].Text);

        var spans = transaction.Spans;
        var chatSpan = spans.FirstOrDefault(s => s.Operation == SentryAIConstants.SpanAttributes.ChatOperation);
        var agentSpan = spans.FirstOrDefault(s => s.Operation == SentryAIConstants.SpanAttributes.InvokeAgentOperation);

        Assert.NotNull(chatSpan);
        Assert.True(chatSpan.IsFinished);
        Assert.Equal(SpanStatus.Ok, chatSpan.Status);
        Assert.Equal(SentryAIConstants.SpanOperations.Chat, chatSpan.Data[SentryAIConstants.SpanAttributes.OperationName]);
        Assert.Equal("Hello World!", chatSpan.Data[SentryAIConstants.SpanAttributes.ResponseText]);

        Assert.NotNull(agentSpan);
        Assert.True(agentSpan.IsFinished);
        Assert.Equal(SpanStatus.Ok, agentSpan.Status);
        Assert.Equal(SentryAIConstants.SpanOperations.InvokeAgent, agentSpan.Data[SentryAIConstants.SpanAttributes.OperationName]);
    }

    [Fact]
    public async Task CompleteStreamingAsync_HandlesErrors_AndFinishesSpanWithException()
    {
        // Arrange
        var transaction = _fixture.Hub.StartTransaction("test-streaming-error", "test");
        _fixture.Hub.ConfigureScope(scope => scope.Transaction = transaction);
        SentrySdk.ConfigureScope(scope => scope.Transaction = transaction);

        var expectedException = new InvalidOperationException("Streaming failed");
        _fixture.InnerClient.GetStreamingResponseAsync(Arg.Any<IList<ChatMessage>>(), Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(CreateFailingStreamingUpdatesAsync(expectedException));
        var sentryChatClient = _fixture.GetSut();

        // Act
        var actualException = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var update in sentryChatClient.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "hi")]))
            {
                // Should not reach here due to exception
            }
        });

        // Assert
        Assert.Equal(expectedException.Message, actualException.Message);

        var chatSpan = transaction.Spans.FirstOrDefault(s => s.Operation == SentryAIConstants.SpanAttributes.ChatOperation);
        var agentSpan = transaction.Spans.FirstOrDefault(s => s.Operation == SentryAIConstants.SpanAttributes.InvokeAgentOperation);

        Assert.NotNull(chatSpan);
        Assert.True(chatSpan.IsFinished);
        Assert.Equal(SpanStatus.InternalError, chatSpan.Status);
        Assert.Equal(SentryAIConstants.SpanOperations.Chat, chatSpan.Data[SentryAIConstants.SpanAttributes.OperationName]);

        Assert.NotNull(agentSpan);
        Assert.True(agentSpan.IsFinished);
        Assert.Equal(SpanStatus.InternalError, agentSpan.Status);
        Assert.Equal(SentryAIConstants.SpanOperations.InvokeAgent, agentSpan.Data[SentryAIConstants.SpanAttributes.OperationName]);
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> CreateFailingStreamingUpdatesAsync(Exception exception)
    {
        yield return new ChatResponseUpdate(ChatRole.System, "Hello");
        await Task.Yield();
        throw exception;
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> CreateTestStreamingUpdatesAsync()
    {
        yield return new ChatResponseUpdate(ChatRole.System, "Hello");
        await Task.Yield();
        yield return new ChatResponseUpdate(ChatRole.System, " World!");
    }
}
