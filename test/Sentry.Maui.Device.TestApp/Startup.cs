#if VISUAL_RUNNER
using DeviceRunners.VisualRunners;
#endif
using DeviceRunners.XHarness;
using Microsoft.Maui.LifecycleEvents;

namespace Sentry.Maui.Device.TestApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var assemblies = new List<Assembly>(
        [
            typeof(Sentry.Tests.SentrySdkTests).Assembly,
            typeof(Sentry.Extensions.Logging.Tests.LogLevelExtensionsTests).Assembly,
            // typeof(Sentry.Maui.Tests.SentryMauiOptionsTests).Assembly,
#if NET9_0_OR_GREATER
            // typeof(Sentry.Maui.CommunityToolkit.Mvvm.Tests.MauiCommunityToolkitMvvmEventsBinderTests).Assembly,
#endif
#if ANDROID
            typeof(Sentry.Android.AssemblyReader.Tests.AndroidAssemblyReaderTests).Assembly,
#endif
        ]);
        var appBuilder = MauiApp.CreateBuilder()
            .ConfigureLifecycleEvents(life =>
            {
#if __ANDROID__
                life.AddAndroid(android =>
                {
                    android.OnCreate(Platform.Init);
                });
#endif
            })
            .UseXHarnessTestRunner(conf =>
            {
                conf.AddTestAssemblies(assemblies);
                conf.AddXunit();
            });

#if VISUAL_RUNNER
        appBuilder.UseVisualTestRunner(conf =>
        {
            conf.AddTestAssemblies(assemblies);
            conf.AddXunit();
        });
#endif

        return appBuilder.Build();
    }
}
