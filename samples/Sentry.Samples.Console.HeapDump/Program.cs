/*
 * This sample demonstrates how you can configure Sentry to automatically capture heap dumps based on certain memory
 * triggers (e.g. if memory consumption exceeds a certain percentage threshold).
 *
 * Note that this functionality is only available when targeting net6.0 or above and is not available on iOS, Android
 * or Mac Catalyst.
 */

using System.Reflection;
using static System.Console;

var cts = new CancellationTokenSource();

// Initialize the Sentry SDK.  (It is not necessary to dispose it.)
SentrySdk.Init(options =>
{
    // You can set here in code, or you can set it in the SENTRY_DSN environment variable.
    // See https://docs.sentry.io/product/sentry-basics/dsn-explainer/
    options.Dsn = "https://eb18e953812b41c3aeb042e666fd3b5c@o447951.ingest.sentry.io/5428537";

    // When debug is enabled, the Sentry client will emit detailed debugging information to the console.
    // This might be helpful, or might interfere with the normal operation of your application.
    // We enable it here for demonstration purposes.
    // You should not do this in your applications unless you are troubleshooting issues with Sentry.
    options.Debug = true;

    // Set TracesSampleRate = 0 to disable tracing for this demo
    options.TracesSampleRate = 0;

    // This option tells Sentry to capture a heap dump and send these as a file attachment in a Sentry event
    options.EnableHeapDumps(
        // Triggers a heap dump if the process uses more than 5% of the total memory. We could use any threshold or even
        // provide a custom trigger function here instead.
        5,
        // Limit the frequency of heap dumps to a maximum of 3 events per day and at least 1 hour between each event.
        Debouncer.PerDay(3, TimeSpan.FromHours(1)),
        // Set the level for heap dump events to Info
        SentryLevel.Info
        );

    // This is an example of intercepting events before they get sent to Sentry. Typically, you might use this to
    // filter events that you didn't want to send but in this case we're using it to detect when a heap dump has
    // been captured, so we know when to stop allocating memory in the heap dump demo.
    options.SetBeforeSend((evt, hint) =>
    {
        if (hint.Attachments.Any(a => a.FileName.EndsWith("gcdump")))
        {
            cts.Cancel();
        }
        return evt; // If we returned null here, that would stop the event from being sent
    });
});

// In Debug mode there will be a bit of stuff logged out during initialization... wait for that to play out
await Task.Delay(1000);

var memoryHog = new List<object>();
WriteLine();

WriteLine("Hogging memory...");

// Sentry checks memory usage every time a full garbage collection occurs. It might take a while to trigger this,
// although we've configured some ridiculously aggressive settings in the runtimeconfig.template.json file to make
// this happen more quickly, for the purposes of this demo... definitely don't do this in production!
while (cts.Token.IsCancellationRequested == false)
{
    var array = new byte[2_000_000_000];
    array.Initialize();
    memoryHog.Add(array);
}

GC.KeepAlive(memoryHog);
