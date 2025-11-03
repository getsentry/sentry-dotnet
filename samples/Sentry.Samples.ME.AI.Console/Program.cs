using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Sentry.Extensions.AI;

using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<Program>();

logger.LogInformation("Starting Microsoft.Extensions.AI sample with Sentry instrumentation");

const string varName = "OPENAI_API_KEY";
var openAiApiKey = Environment.GetEnvironmentVariable(varName);
if (openAiApiKey == null)
{
    throw new InvalidOperationException($"Environment variable for OpenAI API key '{varName}' is not set.");
}

// Create OpenAI API client and wrap it with Sentry instrumentation
var openAiClient = new OpenAI.Chat.ChatClient("gpt-4o-mini", openAiApiKey)
    .AsIChatClient()
    .WithSentry(options =>
    {
#if !SENTRY_DSN_DEFINED_IN_ENV
        // A DSN is required. You can set here in code, or you can set it in the SENTRY_DSN environment variable.
        // See https://docs.sentry.io/product/sentry-basics/dsn-explainer/
        options.Dsn = SamplesShared.Dsn;
#endif
        options.Debug = true;
        options.DiagnosticLevel = SentryLevel.Debug;
        options.SampleRate = 1;
        options.TracesSampleRate = 1;

        // AI-specific settings
        options.RecordInputs = true;
        options.RecordOutputs = true;
        // Since this is a simple console app without Sentry already set up, we need to initialize our SDK
        options.InitializeSdk = true;
    });

var client = new ChatClientBuilder(openAiClient)
    .UseFunctionInvocation()
    .Build();

logger.LogInformation("Making AI call with Sentry instrumentation and tools...");

// This starts a new transaction and attaches it to the scope.
var transaction = SentrySdk.StartTransaction("Program Main", "function");
SentrySdk.ConfigureScope(scope => scope.Transaction = transaction);

var options = new ChatOptions
{
    ModelId = "gpt-4o-mini",
    MaxOutputTokens = 1024,
    Tools =
    [
        // Tool 1: Quick response with minimal delay
        AIFunctionFactory.Create(async (string personName) =>
        {
            logger.LogInformation("GetPersonAge called for {PersonName}", personName);
            await Task.Delay(100); // 100ms delay
            return personName switch
            {
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
        }, "ComplexCalculation", "Performs a complex mathematical calculation. Takes about 1 second to complete.")
    ]
}.WithSentryToolInstrumentation();

var response = await client.GetResponseAsync(
    "Please help me with the following tasks: 1) Find Alice's age, 2) Get weather in New York, and 3) Calculate a complex result for number 15. Please use the appropriate tools for each task.",
    options);

logger.LogInformation("Response: {ResponseText}", response.Messages?.FirstOrDefault()?.Text ?? "No response");

logger.LogInformation("Microsoft.Extensions.AI sample completed! Check your Sentry dashboard for the trace data.");

transaction.Finish();

// Flush Sentry to ensure all transactions are sent before the app exits
await SentrySdk.FlushAsync(TimeSpan.FromSeconds(2));
