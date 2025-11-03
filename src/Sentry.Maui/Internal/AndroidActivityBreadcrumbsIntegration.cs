#if ANDROID
using Microsoft.Maui.LifecycleEvents;
using Activity = Android.App.Activity;

namespace Sentry.Maui.Internal;

// Capture Android Activity lifecycle events as breadcrumbs.
// See: https://github.com/getsentry/sentry-java/blob/ab8a72db41b2e5c66e60cef3102294dddba90b20/sentry-android-core/src/main/java/io/sentry/android/core/ActivityBreadcrumbsIntegration.java
internal static class AndroidActivityBreadcrumbsIntegration
{
    public static void Register(IAndroidLifecycleBuilder lifecycle)
    {
        lifecycle.OnCreate((activity, _) => AddBreadcrumb(activity, "created"));
        lifecycle.OnStart(activity => AddBreadcrumb(activity, "started"));
        lifecycle.OnResume(activity => AddBreadcrumb(activity, "resumed"));
        lifecycle.OnPause(activity => AddBreadcrumb(activity, "paused"));
        lifecycle.OnStop(activity => AddBreadcrumb(activity, "stopped"));
        lifecycle.OnSaveInstanceState((activity, _) => AddBreadcrumb(activity, "saveInstanceState"));
        lifecycle.OnDestroy(activity => AddBreadcrumb(activity, "destroyed"));
    }

    private static void AddBreadcrumb(Activity activity, string state)
    {
        var breadcrumb = new Breadcrumb(
            DateTimeOffset.UtcNow,
            message: null,
            type: MauiEventsBinder.NavigationType,
            data: new Dictionary<string, string>
            {
                { "screen", activity.Class.SimpleName },
                { "state", state }
            },
            category: MauiEventsBinder.LifecycleCategory,
            level: BreadcrumbLevel.Info
        );
        SentrySdk.AddBreadcrumb(breadcrumb);
    }
}
#endif
