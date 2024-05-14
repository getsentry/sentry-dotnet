/*
 * This sample demonstrates how to initialize and enable Open Telemetry with Sentry
 * in a console application.
 * For using Open Telemetry and Sentry in ASP.NET, see Sentry.Samples.OpenTelemetry.AspNet.
 * For using Open Telemetry and Sentry in ASP.NET Core, see Sentry.Samples.OpenTelemetry.AspNetCore.
 */

using System.Diagnostics;
using System.Runtime.InteropServices;
using OpenTelemetry;
using OpenTelemetry.Trace;
using Sentry.OpenTelemetry;
using Sentry.Samples.OpenTelemetry.Console;

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

// Enable our event source listener
var memoryMonitor = new MemoryMonitor(10);
var memoryHog = new List<object>();

// Finally we can use OpenTelemetry to instrument our code. These activities will be captured as a Sentry transaction.
using var activity = activitySource.StartActivity("Main");
Console.WriteLine("Hello World!");

ConsoleKeyInfo key = default;
do
{
    Console.WriteLine("Select an option:");
    Console.WriteLine("c - force garbage collection");
    Console.WriteLine("d - create a memory dump");
    Console.WriteLine("m - hog some more memory");
    Console.WriteLine("q - quit");
    key = Console.ReadKey();
    Console.WriteLine("");
    switch (key.KeyChar)
    {
        case 'c':
            Console.WriteLine("Forcing garbage collection...");
            GC.Collect();
            break;
        case 'd':
            memoryMonitor.CaptureMemoryDump();
            break;
        case 'm':
            Console.WriteLine("Hogging some more memory...");
            for (var i = 0; i < 100000; i++)
            {
                var array = new byte[1024 * 80]; // 80KB
                array.Initialize();
                memoryHog.Add(array);
            }

            break;
        case '\n':
            break;
    }
}
while (key.KeyChar != 'q');
GC.KeepAlive(memoryHog);
GC.KeepAlive(memoryMonitor);

Console.WriteLine("Goodbye cruel world...");
