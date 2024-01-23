namespace Sentry.Samples.MacOS;

[Register("AppDelegate")]
public class AppDelegate : NSApplicationDelegate
{
    public override void DidFinishLaunching(NSNotification notification)
    {
        // Init the Sentry SDK
        SentrySdk.Init(o =>
        {
            o.Debug = true;
            o.Dsn = "https://eb18e953812b41c3aeb042e666fd3b5c@o447951.ingest.sentry.io/5428537";
            o.TracesSampleRate = 1.0;
            o.ProfilesSampleRate = 1.0;
        });
    }
}
