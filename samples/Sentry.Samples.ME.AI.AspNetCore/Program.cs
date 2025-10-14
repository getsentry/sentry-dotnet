#nullable enable
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Anthropic.SDK;
using Anthropic.SDK.Constants;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Sentry.Extensions.AI;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseSentry(options =>
{
#if !SENTRY_DSN_DEFINED_IN_ENV
        // A DSN is required. You can set here in code, or you can set it in the SENTRY_DSN environment variable.
        // See https://docs.sentry.io/product/sentry-basics/dsn-explainer/
        options.Dsn = SamplesShared.Dsn;
#endif

    options.Debug = true;
    options.DiagnosticLevel = SentryLevel.Debug;
    options.SampleRate = 1;
    options.TracesSampleRate = 1.0;
    options.Experimental.EnableLogs = true;
});

var client = new AnthropicClient().Messages
    .AsBuilder()
    .UseFunctionInvocation()
    .Build()
    .WithSentry(agentName: "Anthropic", system: "anthropic");

// Register the Claude API client and Sentry-instrumented chat client
builder.Services.AddKeyedSingleton("claude3_5", client);

var app = builder.Build();

// Endpoint for regular AI chat
app.MapPost("/chat", async (ChatRequest request, IChatClient chatClient, ILogger<Program> logger) =>
{
    logger.LogInformation("Handling chat request with message: {Message}", request.Message);

    try
    {
        var response = await chatClient.GetResponseAsync([
            new ChatMessage(ChatRole.User, request.Message)
        ]);

        return Results.Ok(new { response = response.Messages?.FirstOrDefault()?.Text ?? "No response" });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error processing chat request");
        return Results.Problem("An error occurred while processing your request");
    }
});

// Endpoint for streaming AI chat
// app.MapPost("/chat/stream", async (ChatRequest request, IChatClient chatClient, ILogger<Program> logger) =>
// {
//     logger.LogInformation("Handling streaming chat request with message: {Message}", request.Message);
//
//     return Results.Stream(async stream =>
//     {
//         try
//         {
//             await foreach (var update in chatClient.GetStreamingResponseAsync([
//                 new ChatMessage(ChatRole.User, request.Message)
//             ]))
//             {
//                 if (!string.IsNullOrEmpty(update.Text))
//                 {
//                     var bytes = Encoding.UTF8.GetBytes(update.Text);
//                     await stream.WriteAsync(bytes);
//                     await stream.FlushAsync();
//                 }
//             }
//         }
//         catch (Exception ex)
//         {
//             logger.LogError(ex, "Error processing streaming chat request");
//             var errorBytes = Encoding.UTF8.GetBytes("\n[Error occurred while streaming response]");
//             await stream.WriteAsync(errorBytes);
//         }
//     }, "text/plain");
// });

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

// Simple test endpoint that demonstrates AI integration
app.MapGet("/test", async (IChatClient chatClient, ILogger<Program> logger) =>
{
    logger.LogInformation("Running AI test endpoint");
    ChatOptions options = new()
    {
        ModelId = AnthropicModels.Claude3Haiku,
        MaxOutputTokens = 512,
        Tools = [AIFunctionFactory.Create((string personName) => personName switch {
            "Alice" => "25",
            _ => "40"
        }, "GetPersonAge", "Gets the age of the person whose name is specified.")]
    };

    try
    {
        var response = await chatClient.GetResponseAsync("How old is Alice?", options);

        return Results.Ok(new {
            message = "AI test completed successfully",
            response = response.Messages?.FirstOrDefault()?.Text ?? "No response",
            timestamp = DateTime.UtcNow
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error in AI test endpoint");
        return Results.Problem("An error occurred during the AI test");
    }
});

app.Run();

public record ChatRequest(string Message);

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
