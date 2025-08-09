using Sentry.Extensions.AI;

namespace Sentry.Extensions.AI.Tests;

public class SentryChatClientTests
{
    [Fact]
    public async Task CompleteAsync_CallsInnerClient()
    {
        var inner = Substitute.For<IChatClient>();
        var chatCompletion = new ChatCompletion(new ChatMessage(ChatRole.Assistant, "ok"));
        inner.CompleteAsync(Arg.Any<IList<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(chatCompletion));

        var hub = Substitute.For<IHub>();
        var client = new SentryChatClient(inner, hub, agentName: "Agent", model: "gpt-4o-mini", system: "openai");

        var res = await client.CompleteAsync(new[] { new ChatMessage(ChatRole.User, "hi") }, null);

        Assert.Equal("ok", res.Message.Text);
        await inner.Received(1).CompleteAsync(Arg.Any<IList<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Metadata_ReturnsInnerClientMetadata()
    {
        var inner = Substitute.For<IChatClient>();
        var metadata = new ChatClientMetadata("test-client");
        inner.Metadata.Returns(metadata);

        var hub = Substitute.For<IHub>();
        var client = new SentryChatClient(inner, hub);

        Assert.Equal(metadata, client.Metadata);
    }

    [Fact]
    public async Task CompleteStreamingAsync_CallsInnerClient()
    {
        var inner = Substitute.For<IChatClient>();

        inner.CompleteStreamingAsync(Arg.Any<IList<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>())
            .Returns(CreateTestStreamingUpdates());

        var hub = Substitute.For<IHub>();
        var client = new SentryChatClient(inner, hub, agentName: "Agent", model: "gpt-4o-mini", system: "openai");

        var results = new List<StreamingChatCompletionUpdate>();
        await foreach (var update in client.CompleteStreamingAsync(new[] { new ChatMessage(ChatRole.User, "hi") }, null))
        {
            results.Add(update);
        }

        Assert.Equal(2, results.Count);
        Assert.Equal("Hello", results[0].Text);
        Assert.Equal(" World", results[1].Text);

        inner.Received(1).CompleteStreamingAsync(Arg.Any<IList<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>());
    }

    private static async IAsyncEnumerable<StreamingChatCompletionUpdate> CreateTestStreamingUpdates()
    {
        yield return new StreamingChatCompletionUpdate { Text = "Hello" };
        await Task.Yield(); // Make it actually async
        yield return new StreamingChatCompletionUpdate { Text = " World" };
    }
}


