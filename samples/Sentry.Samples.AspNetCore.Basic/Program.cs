using Microsoft.AspNetCore.Mvc;
using Sentry.Extensibility;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseSentry(options =>
{
    // A DSN is required.  You can set it here, or in configuration, or in an environment variable.
    options.Dsn = "https://eb18e953812b41c3aeb042e666fd3b5c@o447951.ingest.sentry.io/5428537";

    // Enable Sentry performance monitoring
    options.TracesSampleRate = 1.0;
    options.MaxRequestBodySize = RequestSize.Always;

#if DEBUG
    // Log debug information about the Sentry SDK
    options.Debug = true;
#endif
});

// Register HttpClient as a service
builder.Services.AddHttpClient("local", client =>
{
    client.BaseAddress = new Uri("http://localhost:59740");
});

var app = builder.Build();

// An example ASP.NET Core middleware that throws an
// exception when serving a request to path: /throw
app.MapGet("/throw/{message?}", async ([FromServices] IHttpClientFactory clientFactory) =>
{
    try
    {
        var client = clientFactory.CreateClient("local");
        var content = new StringContent("x=1&y=2&", System.Text.Encoding.UTF8, "application/x-www-form-urlencoded");
        var response = await client.PostAsync("/submit", content);
        var responseString = await response.Content.ReadAsStringAsync();

        return Results.Content(responseString, "application/json");
    }
    catch (Exception e)
    {
        throw new Exception("An error occurred while processing the request", e);
    }
});

// POST endpoint to handle form submission
app.MapPost("/submit", async (HttpContext context) =>
{
    await Task.Delay(50); // Simulate a delay
    throw new Exception("Test exception - needs a body");
    // var form = await context.Request.ReadFormAsync();
    // var x = form["x"];
    // var y = form["y"];
    //
    // var log = context.RequestServices.GetRequiredService<ILoggerFactory>()
    //     .CreateLogger<Program>();
    //
    // log.LogInformation("Received form data: x={x}, y={y}", x, y);
    //
    // return Results.Ok(new { x, y });
});

app.Run();
