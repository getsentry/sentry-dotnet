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
            })
            .UseXHarnessTestRunner(conf =>
            {
                conf.AddTestAssemblies([
                    typeof(Sentry.Tests.Ben.BlockingDetector.SuppressBlockingDetectionTests).Assembly,
                ]);
                conf.AddXunit();
            });

        return appBuilder.Build();
    }
}
