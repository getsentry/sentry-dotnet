/*
 * This sample demonstrates how to initialize and enable Open Telemetry with Sentry
 * in a console application.
 * For using Open Telemetry and Sentry in ASP.NET, see Sentry.Samples.OpenTelemetry.AspNet.
 * For using Open Telemetry and Sentry in ASP.NET Core, see Sentry.Samples.OpenTelemetry.AspNetCore.
 */

using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Trace;
using Sentry.OpenTelemetry;

var activitySource = new ActivitySource("Sentry.Samples.OpenTelemetry.Console");

SentrySdk.Init(options =>
{
    // You can set here in code, or you can set it in the SENTRY_DSN environment variable.
    // See https://docs.sentry.io/product/sentry-basics/dsn-explainer/
    options.Dsn = "https://eb18e953812b41c3aeb042e666fd3b5c@o447951.ingest.sentry.io/5428537";

    options.Debug = true;
    options.TracesSampleRate = 1.0;
    options.UseOpenTelemetry(); // <-- Configure Sentry to use OpenTelemetry trace information
});

using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .AddSource(activitySource.Name)
    .AddHttpClientInstrumentation()
    .AddSentry() // <-- Configure OpenTelemetry to send traces to Sentry
    .Build();

Console.WriteLine("Hello World!");

// Finally we can use OpenTelemetry to instrument our code. This activity will be captured as a Sentry transaction.
using (var activity = activitySource.StartActivity("Main"))
{
    // This creates a span called "Task 1" within the transaction
    using (var task = activitySource.StartActivity("Task 1"))
    {
        task?.SetTag("Answer", 42);
        Thread.Sleep(100); // simulate some work
        Console.WriteLine("Task 1 completed");
        task?.SetStatus(Status.Ok);
    }

    // Since we use `AddHttpClientInstrumentation` when initializing OpenTelemetry, the following Http request will also
    // be captured as a Sentry span
    var httpClient = new HttpClient();
    var html = await httpClient.GetStringAsync("https://example.com/");
    Console.WriteLine(html);
}

Console.WriteLine("Goodbye cruel world...");
