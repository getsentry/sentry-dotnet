using DeviceRunners.XHarness;
using Microsoft.Maui.LifecycleEvents;

namespace Sentry.Maui.Device.TestApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
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
                conf.AddTestAssemblies([
                    typeof(Sentry.Tests.SentrySdkTests).Assembly,
                    typeof(Sentry.Extensions.Logging.Tests.LogLevelExtensionsTests).Assembly,
                    typeof(Sentry.Maui.Tests.SentryMauiOptionsTests).Assembly,
#if ANDROID
                    typeof(Sentry.Android.AssemblyReader.Tests.AndroidAssemblyReaderTests).Assembly,
#endif
                ]);
                conf.AddXunit();
            });

        return appBuilder.Build();
    }
}
