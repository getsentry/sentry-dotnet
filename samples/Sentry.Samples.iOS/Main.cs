using Sentry.Samples.iOS;

SentrySdk.Init(o =>
{
    o.Debug = true;
    o.Dsn = "https://eb18e953812b41c3aeb042e666fd3b5c@o447951.ingest.sentry.io/5428537";
});


// This is the main entry point of the application.
// If you want to use a different Application Delegate class from "AppDelegate"
// you can specify it here.
UIApplication.Main(args, null, typeof (AppDelegate));
