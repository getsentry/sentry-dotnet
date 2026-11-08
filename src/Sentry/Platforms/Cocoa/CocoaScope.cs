using Sentry.CocoaSdk;
using Sentry.Extensibility;

namespace Sentry.Cocoa;

internal static class CocoaScope
{
    [ThreadStatic]
    private static int t_configureDepth;

    internal static bool IsConfiguring => t_configureDepth > 0;

    internal static void Configure(
        Action<SentryObjCScope> callback,
        SentryOptions? options = null,
        Action<Action<SentryObjCScope>>? configureNativeScope = null)
    {
        t_configureDepth++;
        try
        {
            (configureNativeScope ?? SentryCocoaSdk.ConfigureScope)(callback);
        }
        catch (Exception e)
        {
            options?.LogError(e, "Failed to configure the native scope.");
        }
        finally
        {
            t_configureDepth--;
        }
    }
}
