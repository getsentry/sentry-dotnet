#nullable enable
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Sentry.Extensions.AI;

SentrySdk.Init(options =>
    {
#if !SENTRY_DSN_DEFINED_IN_ENV
        // A DSN is required. You can set here in code, or you can set it in the SENTRY_DSN environment variable.
        // See https://docs.sentry.io/product/sentry-basics/dsn-explainer/
        options.Dsn = SamplesShared.Dsn;
#endif
        // Set to true to SDK debugging to see the internal messages through the logging library.
        options.Debug = true;
        // Configure the level of Sentry internal logging
        options.DiagnosticLevel = SentryLevel.Debug;
        options.SampleRate = 1;
        options.TracesSampleRate = 1;
    }
);

using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<Program>();

logger.LogInformation("Starting Microsoft.Extensions.AI sample with Sentry instrumentation");

// Create Claude API client and wrap it with Sentry instrumentation
var claudeClient = new ClaudeChatClient();
var chat = claudeClient.WithSentry(agentName: "Anthropic ", system: "anthropic");

logger.LogInformation("Making AI call with Sentry instrumentation...");

var response = await chat.GetResponseAsync([
    new ChatMessage(ChatRole.User, "Say hello from Sentry sample")
]);

logger.LogInformation("Response: {ResponseText}", response.Messages);

// Demonstrate streaming with Sentry instrumentation
logger.LogInformation("Making streaming AI call with Sentry instrumentation...");

var streamingResponse = new List<string>();
await foreach (var update in chat.GetStreamingResponseAsync([
                   new ChatMessage(ChatRole.User, "Say hello and goodbye with streaming")
               ]))
{
    streamingResponse.Add(update.Text ?? "");
}

logger.LogInformation("Streaming Response: {StreamingText}", string.Join("", streamingResponse));

logger.LogInformation("Microsoft.Extensions.AI sample completed! Check your Sentry dashboard for the trace data.");

// Flush Sentry to ensure all transactions are sent before the app exits
await SentrySdk.FlushAsync(TimeSpan.FromSeconds(2));

// Claude API client using HttpClient without third-party dependencies
internal class ClaudeChatClient : IChatClient
{
    private readonly HttpClient _httpClient;
    private const string ApiBaseUrl = "https://api.anthropic.com/v1/messages";

    public ClaudeChatClient()
    {
        var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY") ??
                     throw new InvalidOperationException("ANTHROPIC_API_KEY environment variable is required");

        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
    }

    public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var requestBody = CreateRequestBody(messages, options, false);
        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(ApiBaseUrl, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(responseJson);

        var usage = new UsageDetails();
        var responseText = "No response";
        if (doc.RootElement.TryGetProperty("content", out var contentArray) &&
            contentArray.ValueKind == JsonValueKind.Array)
        {
            var firstContent = contentArray.EnumerateArray().FirstOrDefault();
            if (firstContent.TryGetProperty("text", out var textProperty))
            {
                responseText = textProperty.GetString() ?? "No response";
            }
        }
        if (doc.RootElement.TryGetProperty("usage", out var usageProperty))
        {
            if (usageProperty.TryGetProperty("input_tokens", out var inputTokens) && inputTokens.TryGetInt64(out var inputTokenCount))
            {
                usage.InputTokenCount = inputTokenCount;
            }
            if (usageProperty.TryGetProperty("output_tokens", out var outputTokens) && outputTokens.TryGetInt64(out var outputTokenCount))
            {
                usage.OutputTokenCount = outputTokenCount;
            }
        }

        var responseMessage = new ChatMessage(ChatRole.Assistant, responseText);
        var chatResponse = new ChatResponse(responseMessage)
        {
            Usage = usage
        };

        return chatResponse;
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages,
        ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var requestBody = CreateRequestBody(messages, options, true);
        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(ApiBaseUrl, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (!line.StartsWith("data: ") || line.Length <= 6)
            {
                continue;
            }

            var eventData = line.Substring(6);
            if (eventData == "[DONE]")
            {
                break;
            }

            ClaudeStreamEvent? streamEvent = null;
            try
            {
                streamEvent = JsonSerializer.Deserialize<ClaudeStreamEvent>(eventData);
            }
            catch (JsonException)
            {
                // Skip malformed JSON
                continue;
            }

            if (streamEvent?.Type == "content_block_delta" &&
                streamEvent.Delta?.Type == "text_delta")
            {
                yield return new ChatResponseUpdate(null, streamEvent.Delta.Text);
            }
        }
    }

    private object CreateRequestBody(IEnumerable<ChatMessage> messages, ChatOptions? options, bool stream)
    {
        var claudeMessages = messages
            .Where(m => m.Role != ChatRole.System)
            .Select(m => new
            {
                role = m.Role == ChatRole.User ? "user" : "assistant",
                content = m.Text ?? ""
            })
            .ToArray();

        return new
        {
            model = "claude-3-5-sonnet-20241022",
            max_tokens = options?.MaxOutputTokens ?? 1000,
            messages = claudeMessages,
            stream = stream
        };
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

internal class ClaudeStreamEvent
{
    public string? Type { get; set; }
    public ClaudeStreamDelta? Delta { get; set; }
}

internal class ClaudeStreamDelta
{
    public string? Type { get; set; }
    public string? Text { get; set; }
}
