/*
 * This sample demonstrates the following basic features of Sentry, via a .NET console application:
 * - Error Monitoring (both handled and unhandled exceptions)
 * - Performance Tracing (Transactions / Spans)
 * - Release Health (Sessions)
 * - MSBuild integration for Source Context (see the csproj)
 * - Heap Dumps (.NET 6.0 or later)
 *
 * For more advanced features of the SDK, see Sentry.Samples.Console.Customized.
 */

// Initialize the Sentry SDK.  (It is not necessary to dispose it.)

using System.Net.Http;

CancellationTokenSource cts = new();

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

    // This option is recommended, which enables Sentry's "Release Health" feature.
    options.AutoSessionTracking = true;

    // This option is recommended for client applications only. It ensures all threads use the same global scope.
    // If you are writing a background service of any kind, you should remove this.
    options.IsGlobalModeEnabled = true;

    // This option tells Sentry to capture 100% of traces. You still need to start transactions and spans.
    options.TracesSampleRate = 1.0;

#if NET6_0_OR_GREATER
    // This option tells Sentry to capture a heap dump when the process uses more than 5% of the total memory. The heap
    // dump will be sent to Sentry as a file attachment.
    options.EnableHeapDumps(5);

    // This determines the level of heap dump events that are sent to Sentry
    options.HeapDumpEventLevel = SentryLevel.Warning;

    // A debouncer can be configured to tell Sentry how frequently to send heap dumps. In this case we've configured it
    // to capture a maximum of 3 events per day and to wait at least 1 hour between each event.
    options.HeapDumpDebouncer = Debouncer.PerDay(3, TimeSpan.FromHours(1));

    // This is an example of intercepting events before they get sent to Sentry. Typically, you might use this to
    // filter events that you didn't want to send but in this case we're using it to detect when a heap dump has
    // been captured, so we know when to stop allocating memory in the heap dump demo.
    options.SetBeforeSend((evt, hint) =>
    {
        cts.Cancel();
        return evt; // If we returned null here, that would stop the event from being sent
    });
#endif
});

#if NET6_0_OR_GREATER
    await Task.Delay(1000);
    Console.WriteLine();
    Console.WriteLine("Choose a demo:");
    Console.WriteLine("1. Tracing");
    Console.WriteLine("2. Heap Dump");
    Console.WriteLine("... or press any other key to quit.");
    switch (Console.ReadKey().KeyChar) {
        case '1':
            await TracingDemo();
            break;
        case '2':
            await HeapDumpDemo(cts.Token);
            break;
    }
#else
    await TracingDemo();
#endif

async Task TracingDemo()
{
    // This starts a new transaction and attaches it to the scope.
    var transaction = SentrySdk.StartTransaction("Program Main", "function");
    SentrySdk.ConfigureScope(scope => scope.Transaction = transaction);

    // Do some work. (This is where you'd have your own application logic.)
    await FirstFunction();
    await SecondFunction();
    await ThirdFunction();

    // Always try to finish the transaction successfully.
    // Unhandled exceptions will fail the transaction automatically.
    // Optionally, you can try/catch the exception, and call transaction.Finish(exception) on failure.
    transaction.Finish();

    async Task FirstFunction()
    {
        // This is an example of making an HttpRequest. A trace us automatically captured by Sentry for this.
        var messageHandler = new SentryHttpMessageHandler();
        var httpClient = new HttpClient(messageHandler, true);
        var html = await httpClient.GetStringAsync("https://example.com/");
        Console.WriteLine(html);
    }

    async Task SecondFunction()
    {
        var span = transaction.StartChild("function", nameof(SecondFunction));

        try
        {
            // Simulate doing some work
            await Task.Delay(100);

            // Throw an exception
            throw new ApplicationException("Something happened!");
        }
        catch (Exception exception)
        {
            // This is an example of capturing a handled exception.
            SentrySdk.CaptureException(exception);
            span.Finish(exception);
        }

        span.Finish();
    }

    async Task ThirdFunction()
    {
        var span = transaction.StartChild("function", nameof(ThirdFunction));
        try
        {
            // Simulate doing some work
            await Task.Delay(100);

            // This is an example of an unhandled exception.  It will be captured automatically.
            throw new InvalidOperationException("Something happened that crashed the app!");
        }
        finally
        {
            span.Finish();
        }
    }
}

// Heap dumps are only supported in .NET 6.0 or later.
#if NET6_0_OR_GREATER
async Task HeapDumpDemo(CancellationToken cancellationToken)
{
    var memoryHog = new List<byte[]>();
    Console.WriteLine();
    Console.WriteLine("Hogging memory...");

    // Sentry checks memory usage every time a full garbage collection occurs. It might take a while to trigger this,
    // although we've configured some ridiculously aggressive settings in the runtimeconfig.template.json file to make
    // this happen more quickly, for the purposes of this demo... definitely don't do this in production!
    while (cancellationToken.IsCancellationRequested == false)
    {
        var array = new byte[2_000_000_000];
        array.Initialize();
        memoryHog.Add(array);
    }
    GC.KeepAlive(memoryHog);
    await Task.CompletedTask;
}
#endif
