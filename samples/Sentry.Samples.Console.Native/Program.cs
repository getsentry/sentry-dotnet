/*
 * This sample demonstrates a native crash handling in a NativeAOT published application.
 */
using Sentry;

// Initialize the Sentry SDK.  (It is not necessary to dispose it.)
SentrySdk.Init(options =>
{
    // A Sentry Data Source Name (DSN) is required.
    // See https://docs.sentry.io/product/sentry-basics/dsn-explainer/
    // You can set it in the SENTRY_DSN environment variable, or you can set it in code here.
    // options.Dsn = "... Your DSN ...";

    // When debug is enabled, the Sentry client will emit detailed debugging information to the console.
    // This might be helpful, or might interfere with the normal operation of your application.
    // We enable it here for demonstration purposes.
    // You should not do this in your applications unless you are troubleshooting issues with Sentry.
    options.Debug = true;
});

await FirstFunctionAsync();

async Task FirstFunctionAsync()
{
    await Task.Delay(100);
    await SecondFunctionAsync();
}

async Task SecondFunctionAsync()
{
    await Task.Delay(100);
    #pragma warning disable CS0618
    SentrySdk.CauseCrash(CrashType.Native);
    #pragma warning restore CS0618
}
