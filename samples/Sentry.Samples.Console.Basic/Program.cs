/*
 * This sample demonstrates the following basic features of Sentry, via a .NET console application:
 * - Error Monitoring (both handled and unhandled exceptions)
 * - Performance Tracing (Transactions / Spans)
 * - Release Health (Sessions)
 * - Logs
 * - MSBuild integration for Source Context (see the csproj)
 *
 * For more advanced features of the SDK, see Sentry.Samples.Console.Customized.
 */

using System.Diagnostics;
using System.Net;
using System.Net.Http;
using static System.Console;

// Initialize the Sentry SDK.  (It is not necessary to dispose it.)
SentrySdk.Init(options =>
{
#if !SENTRY_DSN_DEFINED_IN_ENV
    // A DSN is required. You can set here in code, or you can set it in the SENTRY_DSN environment variable.
    // See https://docs.sentry.io/product/sentry-basics/dsn-explainer/
    options.Dsn = SamplesShared.Dsn;
#endif

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

    // This option enables Sentry Logs created via SentrySdk.Logger.
    options.EnableLogs = true;
    options.SetBeforeSendLog(static log =>
    {
        // A demonstration of how you can drop logs based on some attribute they have
        if (log.TryGetAttribute("suppress", out var attribute) && attribute is true)
        {
            return null;
        }

        // Drop logs with level Info
        return log.Level is SentryLogLevel.Info ? null : log;
    });

    // Sentry (trace-connected) Metrics via SentrySdk.Experimental.Metrics are enabled by default.
    options.Experimental.SetBeforeSendMetric<int>(static metric =>
    {
        // A demonstration of how you can modify the metric object before sending it to Sentry
        metric.SetAttribute("operating_system.platform", Environment.OSVersion.Platform.ToString());
        metric.SetAttribute("operating_system.version", Environment.OSVersion.Version.ToString());

        // Return null to drop the metric
        return metric;
    });
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
// Optionally, you can try/catch the exception and call transaction.Finish(exception) on failure.
transaction.Finish();

async Task FirstFunction()
{
    // This is an example of making an HttpRequest. A trace us automatically captured by Sentry for this.
    var messageHandler = new SentryHttpMessageHandler();
    var httpClient = new HttpClient(messageHandler, true);

    var stopwatch = Stopwatch.StartNew();
    var html = await httpClient.GetStringAsync("https://example.com/");
    stopwatch.Stop();

    WriteLine(html);

    // Info-Log filtered via "BeforeSendLog" callback
    SentrySdk.Logger.LogInfo("HTTP Request completed.");

    // Metric modified via "BeforeSendMetric" callback for type "int" before sending it to Sentry
    SentrySdk.Experimental.Metrics.EmitCounter("sentry.samples.console.basic.http_requests_completed", 1);

    // Metric sent as is because no "BeforeSendMetric" is set for type "double"
    SentrySdk.Experimental.Metrics.EmitDistribution("sentry.samples.console.basic.http_request_duration", stopwatch.Elapsed.TotalSeconds, SentryUnits.Duration.Second,
        [new KeyValuePair<string, object>("http.request.method", HttpMethod.Get.Method), new KeyValuePair<string, object>("http.response.status_code", (int)HttpStatusCode.OK)]);
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

        // This is an example of capturing a structured log.
        SentrySdk.Logger.LogError(static log => log.SetAttribute("method", nameof(SecondFunction)),
            "Error with message: {0}", exception.Message);

        span.Finish(exception);
    }
    finally
    {
        span.Finish();
    }
}

async Task ThirdFunction()
{
    // The `using` here ensures the span gets finished when we leave this method... This is unnecessary here,
    // since the method always throws and the span will be finished automatically when the exception is captured,
    // but this gives you another way to ensure spans are finished.
    using var span = transaction.StartChild("function", nameof(ThirdFunction));

    // Simulate doing some work
    await Task.Delay(100);

    // This is an example of a structured log that is filtered via the 'SetBeforeSendLog' delegate above.
    SentrySdk.Logger.LogFatal(static log => log.SetAttribute("suppress", true),
        "Crash imminent!");

    // This is an example of an unhandled exception. It will be captured automatically.
    throw new InvalidOperationException("Something happened that crashed the app!");
}
