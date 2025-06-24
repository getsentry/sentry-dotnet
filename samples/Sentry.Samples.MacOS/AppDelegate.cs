namespace Sentry.Samples.MacOS;

[Register("AppDelegate")]
public class AppDelegate : NSApplicationDelegate
{
    public override void DidFinishLaunching(NSNotification notification)
    {
        // Init the Sentry SDK
        SentrySdk.Init(options =>
        {
#if !SENTRY_DSN_DEFINED_IN_ENV
            // A DSN is required. You can set here in code, or you can set it in the SENTRY_DSN environment variable.
            // See https://docs.sentry.io/product/sentry-basics/dsn-explainer/
            options.Dsn = SamplesShared.Dsn;
#endif
            options.Debug = true;
            options.TracesSampleRate = 1.0;
        });
    }
}
