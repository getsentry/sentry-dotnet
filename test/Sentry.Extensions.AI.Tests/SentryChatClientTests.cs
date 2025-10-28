#nullable enable
using Microsoft.Extensions.AI;

namespace Sentry.Extensions.AI.Tests;

public class SentryChatClientTests
{
    [Fact]
    public async Task CompleteAsync_CallsInnerClient()
    {
        var inner = Substitute.For<IChatClient>();
        var message = new ChatMessage(ChatRole.Assistant, "ok");
        var chatResponse = new ChatResponse(message);
        inner.GetResponseAsync(Arg.Any<IList<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(chatResponse));

        var sentryChatClient = new SentryChatClient(inner);

        var res = await sentryChatClient.GetResponseAsync([new ChatMessage(ChatRole.User, "hi")], null);

        Assert.Equal([message], res.Messages);
        await inner.Received(1).GetResponseAsync(Arg.Any<IList<ChatMessage>>(), Arg.Any<ChatOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompleteStreamingAsync_CallsInnerClient()
    {
        var inner = Substitute.For<IChatClient>();

        inner.GetStreamingResponseAsync(Arg.Any<IList<ChatMessage>>(), Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(CreateTestStreamingUpdatesAsync());

        var client = new SentryChatClient(inner);

        var results = new List<ChatResponseUpdate>();
        await foreach (var update in client.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "hi")], null))
        {
            results.Add(update);
        }

        Assert.Equal(2, results.Count);
        Assert.Equal("Hello", results[0].Text);
        Assert.Equal(" World!", results[1].Text);

        inner.Received(1).GetStreamingResponseAsync(Arg.Any<IList<ChatMessage>>(), Arg.Any<ChatOptions>(),
            Arg.Any<CancellationToken>());
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> CreateTestStreamingUpdatesAsync()
    {
        yield return new ChatResponseUpdate(ChatRole.System, "Hello");
        await Task.Yield(); // Make it async
        yield return new ChatResponseUpdate(ChatRole.System, " World!");
    }
}
