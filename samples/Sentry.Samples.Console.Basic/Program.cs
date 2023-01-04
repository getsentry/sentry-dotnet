using Sentry;

using var _ = SentrySdk.Init(o =>
{
    // The DSN is required.
    o.Dsn = "https://eb18e953812b41c3aeb042e666fd3b5c@o447951.ingest.sentry.io/5428537";

    // When debug is enabled, the Sentry client will emit detailed debugging information to the console.
    o.Debug = true;
});

// The following unhandled exception will be captured and sent to Sentry.
throw new Exception("test");
