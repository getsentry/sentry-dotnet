using Sentry;

using (SentrySdk.Init(o=> { o.Dsn = "https://eb18e953812b41c3aeb042e666fd3b5c@o447951.ingest.sentry.io/5428537"; o.Debug = true; o.DiagnosticLevel = SentryLevel.Debug; }))
{
    // The following exception is captured and sent to Sentry
    throw null;
}
