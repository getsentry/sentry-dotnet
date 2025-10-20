using Anthropic.SDK;
using Anthropic.SDK.Constants;
using Microsoft.Extensions.AI;
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
    .WithSentry(options =>
    {
        options.IncludeAIRequestMessages = false;
        options.IncludeAIResponseContent = false;
    });

// Register the Claude API client and Sentry-instrumented chat client
builder.Services.AddSingleton(client);

var app = builder.Build();

// Simple test endpoint that demonstrates AI integration with multiple tools
app.MapGet("/test", async (IChatClient chatClient, ILogger<Program> logger) =>
{
    logger.LogInformation("Running AI test endpoint with multiple tools");
    var options = new ChatOptions
    {
        ModelId = AnthropicModels.Claude3Haiku,
        MaxOutputTokens = 1024,
        Tools = [
            // Tool 1: Quick response with minimal delay
            AIFunctionFactory.Create(async (string personName) =>
            {
                logger.LogInformation("GetPersonAge called for {PersonName}", personName);
                await Task.Delay(100); // 100ms delay
                return personName switch {
                    "Alice" => "25",
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
                return location.ToLower() switch {
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

            // Tool 4: Tool that can reference other data
            AIFunctionFactory.Create(async (string personName, string location) =>
            {
                logger.LogInformation("GetPersonInfo called for {PersonName} in {Location}", personName, location);
                await Task.Delay(300); // 300ms delay
                var age = personName switch {
                    "Alice" => "25",
                    "Bob" => "30",
                    "Charlie" => "35",
                    _ => "40"
                };
                var weather = location.ToLower() switch {
                    "new york" => "Sunny, 72°F",
                    "london" => "Cloudy, 60°F",
                    "tokyo" => "Rainy, 68°F",
                    _ => "Unknown weather conditions"
                };
                return $"{personName} (age {age}) is experiencing {weather} weather in {location}";
            }, "GetPersonInfo", "Gets comprehensive info about a person in a specific location by combining age and weather data. Takes about 300ms."),

            // Tool 5: Data aggregation tool that requests individual ages
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
            }, "CalculateAverageAge", "Calculates the average from a list of ages. You should first get individual ages using GetPersonAge, then use this tool to calculate the average. Takes about 200ms to complete.")
        ]
    }.WithSentry();

    try
    {
        var response = await chatClient.GetResponseAsync(
            "Please help me with the following tasks: 1) Find Alice's age, 2) Get weather in New York, 3) Calculate a complex result for number 15, 4) Get comprehensive info for Bob in London, and 5) Calculate average age for Alice, Bob, and Charlie (first get each person's age individually using GetPersonAge, then use CalculateAverageAge with those results). Please use the appropriate tools for each task and demonstrate tool chaining where needed.",
            options);

        return Results.Ok(new {
            message = "AI test with multiple tools completed successfully",
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
