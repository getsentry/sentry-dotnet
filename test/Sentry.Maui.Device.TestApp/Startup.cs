using Microsoft.Maui.TestUtils.DeviceTests.Runners;

namespace Sentry.Maui.Device.TestApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var appBuilder = MauiApp.CreateBuilder();
            appBuilder
                .ConfigureTests(new TestOptions
                {
                    Assemblies =
                    {
                        typeof(Sentry.Tests.SentrySdkTests).Assembly,
                        typeof(Sentry.Maui.Tests.ApiApprovalTests).Assembly
                    },
                })
                .UseHeadlessRunner(new HeadlessRunnerOptions
                {
                    RequiresUIContext = true,
                })
                .UseVisualRunner();

            return appBuilder.Build();
        }
    }
}
