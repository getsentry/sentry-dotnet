using Microsoft.Extensions.AI;

namespace Sentry.Extensions.AI.Tests;

public class SentryChatClientTests : IDisposable
{
    private class Fixture
    {
        public SentryOptions Options { get; }
        public IHub Hub { get; }
        public ActivitySource Source { get; } = SentryAIActivitySource.CreateSource();
        public IChatClient InnerClient { get; } = Substitute.For<IChatClient>();

        public Fixture()
        {
            Options = new SentryOptions
            {
                Dsn = ValidDsn,
                TracesSampleRate = 1.0,
            };

            Hub = new Hub(Options);
        }

        public SentryChatClient GetSut(Action<SentryAIOptions>? configure = null) =>
            new(Source, Hub, InnerClient, configure);
    }

    private readonly Fixture _fixture = new();

    public void Dispose()
    {
        ((Hub)_fixture.Hub).Dispose();
    }

    [Fact]
    public async Task CompleteAsync_CallsInnerClient_AndSetsData()
    {
        // Arrange
        var transaction = _fixture.Hub.StartTransaction("test-nonstreaming", "test");
        _fixture.Hub.ConfigureScope(scope => scope.Transaction = transaction);

        var message = new ChatMessage(ChatRole.Assistant, "ok");
        var chatResponse = new ChatResponse(message);

        _fixture.InnerClient.GetResponseAsync(Arg.Any<IList<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(chatResponse));
        var sentryChatClient = _fixture.GetSut(RecordInputsAndOutputs);

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
        Assert.True(chatSpan.Data.ContainsKey(SentryAIConstants.SpanAttributes.RequestMessages));

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
        // Simulate FunctionInvokingChatClient's Activity
        var listener = SentryAIActivityListener.CreateListener(_fixture.Hub);
        var activity = _fixture.Source.StartActivity(SentryAIConstants.FICCActivityNames[0]);
        var sentryChatClient = _fixture.GetSut();
        var chatClientOption = new ChatOptions
        {
            Tools = []
        };
        var expectedException = new InvalidOperationException("Streaming failed");

        _fixture.InnerClient.GetResponseAsync(Arg.Any<IList<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>())
            .Throws(expectedException);

        // Act
        var res = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await sentryChatClient.GetResponseAsync([new ChatMessage(ChatRole.User, "hi")], chatClientOption));
        activity?.Stop();
        listener.Dispose();

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
        // Arrange
        var transaction = _fixture.Hub.StartTransaction("test-streaming", "test");
        _fixture.Hub.ConfigureScope(scope => scope.Transaction = transaction);

        _fixture.InnerClient.GetStreamingResponseAsync(Arg.Any<IList<ChatMessage>>(), Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(CreateTestStreamingUpdatesAsync());
        var sentryChatClient = _fixture.GetSut(RecordInputsAndOutputs);
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

        var expectedException = new InvalidOperationException("Streaming failed");
        _fixture.InnerClient.GetStreamingResponseAsync(Arg.Any<IList<ChatMessage>>(), Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(CreateFailingStreamingUpdatesAsync(expectedException));
        var sentryChatClient = _fixture.GetSut();
        var results = new List<ChatResponseUpdate>();

        // Act
        var actualException = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var update in sentryChatClient.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "hi")]))
            {
                results.Add(update);
            }
            throw new UnreachableException("Should not reach here due to exception.");
        });

        // Assert
        Assert.Equal(expectedException.Message, actualException.Message);
        var result = Assert.Single(results);
        Assert.Equal("Hello", result.Text);

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

    [Fact]
    public async Task CompleteAsync_RecordingNotConfigured_DoesNotRecordContent()
    {
        // Arrange
        var transaction = _fixture.Hub.StartTransaction("test-nonstreaming", "test");
        _fixture.Hub.ConfigureScope(scope => scope.Transaction = transaction);
        _fixture.InnerClient.GetResponseAsync(Arg.Any<IList<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok"))));
        var sentryChatClient = _fixture.GetSut();

        // Act
        await sentryChatClient.GetResponseAsync([new ChatMessage(ChatRole.User, "hi")]);

        // Assert
        var aiSpans = transaction.Spans.Where(s => s.Operation is SentryAIConstants.SpanAttributes.ChatOperation
            or SentryAIConstants.SpanAttributes.InvokeAgentOperation).ToList();
        aiSpans.Should().HaveCount(2);
        aiSpans.Should().AllSatisfy(span =>
        {
            span.Data.Should().NotContainKey(SentryAIConstants.SpanAttributes.RequestMessages);
            span.Data.Should().NotContainKey(SentryAIConstants.SpanAttributes.ResponseText);
            span.Data.Should().ContainKey(SentryAIConstants.SpanAttributes.UsageTotalTokens);
        });
    }

    [Fact]
    public async Task CompleteAsync_DataCollectionGenAIEnabled_RecordsContent()
    {
        // Arrange
        _fixture.Options.ConfigureDataCollection(dataCollection =>
        {
            dataCollection.GenAI.Inputs = true;
            dataCollection.GenAI.Outputs = true;
        });
        var transaction = _fixture.Hub.StartTransaction("test-nonstreaming", "test");
        _fixture.Hub.ConfigureScope(scope => scope.Transaction = transaction);
        _fixture.InnerClient.GetResponseAsync(Arg.Any<IList<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok"))));
        var sentryChatClient = _fixture.GetSut();

        // Act
        await sentryChatClient.GetResponseAsync([new ChatMessage(ChatRole.User, "hi")]);

        // Assert
        var chatSpan = transaction.Spans.Single(s => s.Operation == SentryAIConstants.SpanAttributes.ChatOperation);
        chatSpan.Data.Should().ContainKey(SentryAIConstants.SpanAttributes.RequestMessages);
        chatSpan.Data.Should().ContainKey(SentryAIConstants.SpanAttributes.ResponseText).WhoseValue.Should().Be("ok");
    }

    [Fact]
    public async Task CompleteStreamingAsync_RecordingNotConfigured_DoesNotRecordContent()
    {
        // Arrange
        var transaction = _fixture.Hub.StartTransaction("test-streaming", "test");
        _fixture.Hub.ConfigureScope(scope => scope.Transaction = transaction);
        _fixture.InnerClient.GetStreamingResponseAsync(Arg.Any<IList<ChatMessage>>(), Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(CreateTestStreamingUpdatesAsync());
        var sentryChatClient = _fixture.GetSut();

        // Act
        await foreach (var _ in sentryChatClient.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "hi")]))
        {
        }

        // Assert
        var chatSpan = transaction.Spans.Single(s => s.Operation == SentryAIConstants.SpanAttributes.ChatOperation);
        chatSpan.Data.Should().NotContainKey(SentryAIConstants.SpanAttributes.RequestMessages);
        chatSpan.Data.Should().NotContainKey(SentryAIConstants.SpanAttributes.ResponseText);
    }

    private static void RecordInputsAndOutputs(SentryAIOptions options)
    {
        options.Experimental.RecordInputs = true;
        options.Experimental.RecordOutputs = true;
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
