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
});

