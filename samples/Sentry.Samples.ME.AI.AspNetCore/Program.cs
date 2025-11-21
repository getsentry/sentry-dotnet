using Microsoft.Extensions.AI;

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
    options.EnableLogs = true;
});

// This sample uses Microsoft.Extensions.AI.OpenAI
// Check whether OPENAI_API_KEY env var exists
const string varName = "OPENAI_API_KEY";
var openAiApiKey = Environment.GetEnvironmentVariable(varName);
if (openAiApiKey == null)
{
    throw new InvalidOperationException($"Environment variable for OpenAI API key '{varName}' is not set.");
}

var openAiClient = new OpenAI.Chat.ChatClient("gpt-4o-mini", openAiApiKey)
    .AsIChatClient()
    .AddSentry(options =>
    {
        options.Experimental.RecordInputs = true;
        options.Experimental.RecordOutputs = true;
    });

var client = new ChatClientBuilder(openAiClient)
    .UseFunctionInvocation()
    .Build();

// Register the OpenAI API client and Sentry-instrumented chat client
builder.Services.AddSingleton(client);

var app = builder.Build();

// Simple test endpoint that demonstrates AI integration with multiple tools
app.MapGet("/test", async (IChatClient chatClient, ILogger<Program> logger) =>
{
    logger.LogInformation("Running AI test endpoint with multiple tools");
    var testOptions = GetOptions(logger);

    try
    {
        var streamingResponse = new List<string>();
        await foreach (var update in chatClient.GetStreamingResponseAsync([
                           new ChatMessage(ChatRole.User,
                               """
                               Please help me with the following tasks:
                               1) Find Alice's age,
                               2) Get weather in New York,
                               3) Calculate a complex result for number 15,
                               4) Calculate average age for Alice, Bob, and Charlie
                               (first get each person's age individually using GetPersonAge, then use CalculateAverageAge with those results).
                               Please use the appropriate tools for each task and demonstrate tool chaining where needed.
                               """)
                       ], testOptions))
        {
            if (!string.IsNullOrEmpty(update.Text))
            {
                streamingResponse.Add(update.Text);
            }
        }

        var fullResponse = string.Concat(streamingResponse);

        return Results.Ok(new
        {
            message = "AI test with multiple tools completed successfully (streaming)",
            response = fullResponse,
            timestamp = DateTime.UtcNow
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error in AI test endpoint");
        return Results.Problem("An error occurred during the AI test");
    }
});

app.MapGet("/throw", async (IChatClient chatClient, ILogger<Program> logger) =>
{
    logger.LogInformation("Running AI test endpoint with a tool that will throw an exception");
    var throwOptions = GetOptions(logger);

    try
    {
        var update = await chatClient.GetResponseAsync([
                           new ChatMessage(ChatRole.User,
                               """
                               Please run these tools in order:
                               1) Calculate a complex result for number 15,
                               2) the Mysterious tool and tell me what that that returns.
                               """)
        ], throwOptions);

        return Results.Ok(new
        {
            message = "AI test with multiple tools completed successfully (streaming)",
            response = update.Text,
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
return;

ChatOptions GetOptions(ILogger logger)
{
    return new ChatOptions
    {
        ModelId = "gpt-4o-mini",
        MaxOutputTokens = 1024,
        Tools =
        [
            // Tool 1: Quick response with minimal delay, but throws an error when trying to get Alice's age
            AIFunctionFactory.Create(async (string personName) =>
            {
                logger.LogInformation("GetPersonAge called for {PersonName}", personName);
                await Task.Delay(100); // 100ms delay
                return personName switch
                {
                    "Bob" => "30",
                    "Charlie" => "35",
                    _ => "40"
                };
            }, "GetPersonAge", "Gets the age of the person whose name is specified. Takes about 100ms to complete."),

            // Tool 2: Medium delay tool for weather
            AIFunctionFactory.Create(async (string location) =>
            {
                logger.LogInformation("GetWeather called for {Location}", location);
                await Task.Delay(500); // 500ms delay
                return location.ToLower() switch
                {
                    "new york" => "Sunny, 72°F",
                    "london" => "Cloudy, 60°F",
                    "tokyo" => "Rainy, 68°F",
                    _ => "Unknown weather conditions"
                };
            }, "GetWeather", "Gets the current weather for a location. Takes about 500ms to complete."),

            // Tool 3: Slow tool for complex calculation
            AIFunctionFactory.Create(async (int number) =>
            {
                logger.LogInformation("ComplexCalculation called with {Number}", number);
                await Task.Delay(1000); // 1000ms delay
                var result = (number * number) + (number * 10);
                return $"Complex calculation result for {number}: {result}";
            }, "ComplexCalculation", "Performs a complex mathematical calculation. Takes about 1 second to complete."),

            // Tool 4: Data aggregation tool that requests individual ages
            AIFunctionFactory.Create(async (int[] ages) =>
            {
                logger.LogInformation("CalculateAverageAge called with ages: {Ages}", string.Join(", ", ages));
                await Task.Delay(200); // 200ms delay for calculation
                if (ages.Length == 0)
                {
                    return "No ages provided";
                }

                var average = ages.Average();
                return $"Average age calculated: {average:F1} years from {ages.Length} people. Individual ages: {string.Join(", ", ages)}";
            }, "CalculateAverageAge", "Calculates the average from a list of ages. You should first get individual ages using GetPersonAge, then use this tool to calculate the average. Takes about 200ms to complete."),

            // Tool 5: Tool that will throw an error
            AIFunctionFactory.Create(async () =>
            {
                logger.LogInformation("Mysterious tool called");
                await Task.Delay(2000);
                throw new TimeoutException("Mysterious tool called, but returned an error :(");
            }, "MysteriousTool", "May return an error...")
        ]
    }.AddSentryToolInstrumentation();
}
