using Sentry.iOS;

namespace Sentry;

public static partial class SentrySdk
{
    static partial void InitNative(SentryOptions options)
    {
        SentrySDK.StartWithConfigureOptions(o =>
        {
            o.Dsn = options.Dsn;
            o.Debug = options.Debug;
        });

        SentrySDK.CaptureMessage("Testing binding to iOS");
    }
}
