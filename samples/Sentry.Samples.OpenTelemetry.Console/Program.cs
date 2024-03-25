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
    // options.Dsn = "... Your DSN ...";
    options.TracesSampleRate = 1.0;
    options.UseOpenTelemetry(); // <-- Configure Sentry to use OpenTelemetry trace information
});

using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .AddSource(activitySource.Name)
    .AddSentry() // <-- Configure OpenTelemetry to send traces to Sentry
    .Build();

// Finally we can use OpenTelemetry to instrument our code. These activities will be captured as a Sentry transaction.
using (var activity = activitySource.StartActivity("Main"))
{
    Console.WriteLine("Hello World!");
    using (var task = activitySource.StartActivity("Task 1"))
    {
        task?.SetTag("Answer", 42);
        Thread.Sleep(100); // simulate some work
        Console.WriteLine("Task 1 completed");
        task.SetStatus(Status.Ok);
    }

    using (var task = activitySource.StartActivity("Task 2"))
    {
        task?.SetTag("Question", "???");
        Thread.Sleep(100); // simulate some more work
        Console.WriteLine("Task 2 unresolved");
        task?.SetStatus(Status.Error);
    }
    Console.WriteLine("Goodbye cruel world...");
}
