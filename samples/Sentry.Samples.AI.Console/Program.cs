using Sentry.Extensions.AI;
using Sentry.Extensibility;

var services = new ServiceCollection();

// Initialize Sentry - get DSN from environment variable (set SENTRY_DSN env var with your DSN)
var sentryDsn = Environment.GetEnvironmentVariable("SENTRY_DSN") ?? "https://test@localhost/1"; // placeholder
Console.WriteLine($"Using Sentry DSN: {sentryDsn}");

using var sentryInstance = SentrySdk.Init(options =>
{
    options.Dsn = sentryDsn;
    options.Debug = true; // Enable for demo
    options.TracesSampleRate = 1.0;
});

// Register Sentry's IHub for DI
services.AddSingleton<IHub>(_ => HubAdapter.Instance);

var sp = services.BuildServiceProvider();

// Create a simple echo client and wrap it with Sentry instrumentation
var echoClient = new EchoChatClient();
var hub = sp.GetRequiredService<IHub>();
var chat = echoClient.WithSentry(hub, agentName: "Sample Agent", model: "gpt-4o-mini", system: "openai");

Console.WriteLine("Making AI call with Sentry instrumentation...");

var response = await chat.CompleteAsync(new[]
{
    new ChatMessage(ChatRole.User, "Say hello from Sentry sample")
});

Console.WriteLine($"Response: {response.Message.Text}");
Console.WriteLine("Sample completed! Check your Sentry dashboard for the trace data.");

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

    public IAsyncEnumerable<StreamingChatCompletionUpdate> CompleteStreamingAsync(IList<ChatMessage> messages, ChatOptions options = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public TService GetService<TService>(object key = null) where TService : class => null;
    public void Dispose() { }
}


