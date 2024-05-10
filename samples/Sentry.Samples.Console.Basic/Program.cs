/*
 * This sample demonstrates the following basic features of Sentry, via a .NET console application:
 * - Error Monitoring (both handled and unhandled exceptions)
 * - Performance Tracing (Transactions / Spans)
 * - Release Health (Sessions)
 * - MSBuild integration for Source Context (see the csproj)
 *
 * For more advanced features of the SDK, see Sentry.Samples.Console.Customized.
 */

// Initialize the Sentry SDK.  (It is not necessary to dispose it.)

using System.Net.Http;

SentrySdk.Init(options =>
{
    // TODO: Configure a Sentry Data Source Name (DSN).
    // See https://docs.sentry.io/product/sentry-basics/dsn-explainer/
    // You can set it in the SENTRY_DSN environment variable, or you can set it in code here.
    options.Dsn = "... Your DSN ...";

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

    // This option will enable Sentry's tracing features. You still need to start transactions and spans.
    options.EnableTracing = true;
});

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
    var httpClient = new  HttpClient(messageHandler, true);
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
