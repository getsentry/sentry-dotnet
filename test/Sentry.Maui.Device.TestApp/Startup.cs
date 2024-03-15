using Microsoft.Maui.LifecycleEvents;
using Microsoft.Maui.TestUtils.DeviceTests.Runners;

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
                    android.OnCreate((activity, bundle) =>
                        Platform.Init(activity, bundle));
                });
#endif
            })
            .ConfigureTests(new TestOptions
            {
                // This is the list of assemblies containing tests that will be run
                Assemblies =
                {
                    typeof(Sentry.Tests.SentrySdkTests).Assembly,
                    typeof(Sentry.Extensions.Logging.Tests.LogLevelExtensionsTests).Assembly,
                    typeof(Sentry.Maui.Tests.SentryMauiOptionsTests).Assembly,
#if ANDROID
                    typeof(Sentry.Android.AssemblyReader.Tests.AndroidAssemblyReaderTests).Assembly,
#endif
                }
            })
            .UseHeadlessRunner(new HeadlessRunnerOptions
            {
                RequiresUIContext = true,
            })
            .UseVisualRunner();

        return appBuilder.Build();
    }
}
