namespace Sentry.Samples.MacOS;

[Register("AppDelegate")]
public class AppDelegate : NSApplicationDelegate
{
    public override void DidFinishLaunching(NSNotification notification)
    {
        // Init the Sentry SDK
        SentrySdk.Init(options =>
        {
            options.Dsn = "https://eb18e953812b41c3aeb042e666fd3b5c@o447951.ingest.sentry.io/5428537";
            options.Debug = true;
            options.TracesSampleRate = 1.0;
        });
    }
}
