using Sentry.iOS;

namespace Sentry;

public static partial class SentrySdk
{
    static partial void InitNative(SentryOptions options)
    {
        var o = new Sentry.iOS.SentryOptions();
        o.Dsn = options.Dsn;
        o.Debug = options.Debug;
        SentrySDK.StartWithOptionsObject(o);

        // Blocked not getting executed:
        // SentrySDK.StartWithConfigureOptions(o =>
        // {
        //     o.Dsn = options.Dsn;
        //     o.Debug = options.Debug;
        // });

        SentrySDK.CaptureMessage("Testing binding to iOS");
    }
}
