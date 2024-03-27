/*
 * This sample demonstrates how to initialize and enable Open Telemetry with Sentry
 * in a console application.
 * For using Open Telemetry and Sentry in ASP.NET, see Sentry.Samples.OpenTelemetry.AspNet.
 * For using Open Telemetry and Sentry in ASP.NET Core, see Sentry.Samples.OpenTelemetry.AspNetCore.
 */

using OpenTelemetry;
using OpenTelemetry.Trace;
using Sentry.OpenTelemetry;

var activitySource = Sentry.OpenTelemetry.TracerProviderBuilderExtensions.DefaultActivitySource;

SentrySdk.Init(options =>
{
    // options.Dsn = "... Your DSN ...";
    options.Dsn = "https://eb18e953812b41c3aeb042e666fd3b5c@o447951.ingest.sentry.io/5428537";
    options.TracesSampleRate = 1.0;
    options.UseOpenTelemetry(); // <-- Configure Sentry to use OpenTelemetry trace information
});

// using var tracerProvider = Sdk.CreateTracerProviderBuilder()
//     .AddSentry() // <-- Configure OpenTelemetry to send traces to Sentry
//     .Build();

Console.WriteLine("Hello World!");
Question();
Console.WriteLine("Goodbye cruel world...");

void Question()
{
    using var disposable = SentrySdk.PushScope();
    SentrySdk.ConfigureScope(scope =>
    {
        scope.AddBreadcrumb("Asking the question...");
        scope.SetTag("Question", "Meaning of life");
        using var task = activitySource.StartActivity("Question");
        Thread.Sleep(100); // simulate some work
        Answer();
    });
}

void Answer()
{
    SentrySdk.ConfigureScope(scope =>
    {
        scope.AddBreadcrumb("Giving the answer...");
        scope.SetTag("Answer", "42");
        using var task = activitySource.StartActivity("Answer");
        Thread.Sleep(100); // simulate some work
    });
}
