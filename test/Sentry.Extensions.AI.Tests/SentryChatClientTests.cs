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
}


