using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sentry.Extensions.AI;
using System.Runtime.CompilerServices;

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.AddSentry(options =>
    {
#if !SENTRY_DSN_DEFINED_IN_ENV
        // A DSN is required. You can set here in code, or you can set it in the SENTRY_DSN environment variable.
        // See https://docs.sentry.io/product/sentry-basics/dsn-explainer/
        options.Dsn = SamplesShared.Dsn;
#endif

        // Set to true to SDK debugging to see the internal messages through the logging library.
        options.Debug = false;

        // Enable performance monitoring for AI traces, 1.0 = 100%
        options.TracesSampleRate = 1.0;
    });
});
var logger = loggerFactory.CreateLogger<Program>();

logger.LogInformation("Starting Microsoft.Extensions.AI sample with Sentry instrumentation");

// Create a simple echo client and wrap it with Sentry instrumentation
var echoClient = new EchoChatClient();
var chat = echoClient.WithSentry(agentName: "ME.AI Sample Agent", model: "gpt-4o-mini", system: "openai");

logger.LogInformation("Making AI call with Sentry instrumentation...");

var response = await chat.CompleteAsync(new[]
{
    new ChatMessage(ChatRole.User, "Say hello from Sentry sample")
});

logger.LogInformation("Response: {ResponseText}", response.Message.Text);

// Demonstrate streaming with Sentry instrumentation
logger.LogInformation("Making streaming AI call with Sentry instrumentation...");

var streamingResponse = new List<string>();
await foreach (var update in chat.CompleteStreamingAsync(new[]
{
    new ChatMessage(ChatRole.User, "Say hello and goodbye with streaming")
}))
{
    streamingResponse.Add(update.Text ?? "");
}

logger.LogInformation("Streaming Response: {StreamingText}", string.Join("", streamingResponse));

logger.LogInformation("Microsoft.Extensions.AI sample completed! Check your Sentry dashboard for the trace data.");

// Simple echo client for demonstration
public class EchoChatClient : IChatClient
{
    public ChatClientMetadata Metadata => new("echo-client");

    public Task<ChatCompletion> CompleteAsync(IList<ChatMessage> messages, ChatOptions options = null, CancellationToken cancellationToken = default)
    {
        var lastMessage = messages.LastOrDefault()?.Text ?? "Hello from echo client!";
        var responseMessage = new ChatMessage(ChatRole.Assistant, $"Echo: {lastMessage}");
        return Task.FromResult(new ChatCompletion(responseMessage));
    }

    public async IAsyncEnumerable<StreamingChatCompletionUpdate> CompleteStreamingAsync(IList<ChatMessage> messages, ChatOptions options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var lastMessage = messages.LastOrDefault()?.Text ?? "Hello from echo client!";
        var parts = new[] { "Echo: ", lastMessage.Substring(0, Math.Min(10, lastMessage.Length)), "...", " (streaming)" };
        
        foreach (var part in parts)
        {
            await Task.Delay(100, cancellationToken); // Simulate streaming delay
            yield return new StreamingChatCompletionUpdate { Text = part };
        }
    }

    public TService GetService<TService>(object key = null) where TService : class => null;
    public void Dispose() { }
}


