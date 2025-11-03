#nullable enable
using Microsoft.Extensions.AI;

namespace Sentry.Extensions.AI.Tests;

public class SentryChatClientTests
{
    private class Fixture
    {
        private SentryOptions Options { get; }
        public ISentryClient Client { get; }
        public IHub Hub { get; set; }

        public Fixture()
        {
            Options = new SentryOptions
            {
                Dsn = ValidDsn,
                TracesSampleRate = 1.0,
            };

            SentrySdk.Init(Options);
            Hub = SentrySdk.CurrentHub;
            Client = Substitute.For<ISentryClient>();
        }
    }

    private readonly Fixture _fixture = new();

    [Fact]
    public async Task CompleteAsync_CallsInnerClient_AndSetsData()
    {
        // Arrange
        var transaction = _fixture.Hub.StartTransaction("test-nonstreaming", "test");
        _fixture.Hub.ConfigureScope(scope => scope.Transaction = transaction);
        SentrySdk.ConfigureScope(scope => scope.Transaction = transaction);

        var inner = Substitute.For<IChatClient>();
        var sentryChatClient = new SentryChatClient(inner);
        var message = new ChatMessage(ChatRole.Assistant, "ok");
        var chatResponse = new ChatResponse(message);

        inner.GetResponseAsync(Arg.Any<IList<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(chatResponse));

        // Act
        var res = await sentryChatClient.GetResponseAsync([new ChatMessage(ChatRole.User, "hi")]);

        // Assert
        Assert.Equal([message], res.Messages);
        await inner.Received(1).GetResponseAsync(Arg.Any<IList<ChatMessage>>(), Arg.Any<ChatOptions>(),
            Arg.Any<CancellationToken>());
        var spans = transaction.Spans;
        var chatSpan = spans.FirstOrDefault(s => s.Operation == SentryAIConstants.SpanAttributes.ChatOperation);
        Assert.NotNull(chatSpan);
        Assert.Equal(SpanStatus.Ok, chatSpan.Status);
        Assert.True(chatSpan.IsFinished);
        Assert.Equal("chat", chatSpan.Data[SentryAIConstants.SpanAttributes.OperationName]);
        Assert.Equal("ok", chatSpan.Data[SentryAIConstants.SpanAttributes.ResponseText]);
    }

    [Fact]
    public async Task CompleteStreamingAsync_CallsInnerClient_AndSetsSpanData()
    {
        // Arrange - Use Fixture Hub to start transaction
        var transaction = _fixture.Hub.StartTransaction("test-streaming", "test");
        _fixture.Hub.ConfigureScope(scope => scope.Transaction = transaction);
        SentrySdk.ConfigureScope(scope => scope.Transaction = transaction);

        var inner = Substitute.For<IChatClient>();
        inner.GetStreamingResponseAsync(Arg.Any<IList<ChatMessage>>(), Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(CreateTestStreamingUpdatesAsync());
        var client = new SentryChatClient(inner);
        var results = new List<ChatResponseUpdate>();

        // Act
        await foreach (var update in client.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "hi")]))
        {
            results.Add(update);
        }

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Equal("Hello", results[0].Text);
        Assert.Equal(" World!", results[1].Text);
        inner.Received(1).GetStreamingResponseAsync(Arg.Any<IList<ChatMessage>>(), Arg.Any<ChatOptions>(),
            Arg.Any<CancellationToken>());
        var spans = transaction.Spans;
        var chatSpan = spans.FirstOrDefault(s => s.Operation == SentryAIConstants.SpanAttributes.ChatOperation);
        Assert.NotNull(chatSpan);
        Assert.Equal(SpanStatus.Ok, chatSpan.Status);
        Assert.True(chatSpan.IsFinished);
        Assert.Equal("chat", chatSpan.Data[SentryAIConstants.SpanAttributes.OperationName]);
        Assert.Equal("Hello World!", chatSpan.Data[SentryAIConstants.SpanAttributes.ResponseText]);
    }

    [Fact]
    public async Task CompleteStreamingAsync_HandlesErrors_AndFinishesSpanWithException()
    {
        // Arrange
        var transaction = _fixture.Hub.StartTransaction("test-streaming-error", "test");
        _fixture.Hub.ConfigureScope(scope => scope.Transaction = transaction);
        SentrySdk.ConfigureScope(scope => scope.Transaction = transaction);

        var inner = Substitute.For<IChatClient>();
        var expectedException = new InvalidOperationException("Streaming failed");
        inner.GetStreamingResponseAsync(Arg.Any<IList<ChatMessage>>(), Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(CreateFailingStreamingUpdatesAsync(expectedException));
        var client = new SentryChatClient(inner);

        // Act
        var actualException = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var update in client.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "hi")]))
            {
                // Should not reach here due to exception
            }
        });

        Assert.Equal(expectedException.Message, actualException.Message);

        // Assert
        var spans = transaction.Spans;
        var chatSpan = spans.FirstOrDefault(s => s.Operation == SentryAIConstants.SpanAttributes.ChatOperation);
        Assert.NotNull(chatSpan);
        Assert.Equal(SpanStatus.InternalError, chatSpan.Status);
        Assert.True(chatSpan.IsFinished);
        Assert.Equal("chat", chatSpan.Data[SentryAIConstants.SpanAttributes.OperationName]);
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
