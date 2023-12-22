using System.Net;
using Microsoft.AspNetCore;

namespace Samples.AspNetCore.Mvc;

public static class Program
{
    public static void Main(string[] args)
    {
        BuildWebHost(args).Run();
    }

    public static IWebHost BuildWebHost(string[] args) =>
        WebHost.CreateDefaultBuilder(args)
            .UseShutdownTimeout(TimeSpan.FromSeconds(10))
            .UseStartup<Startup>()

            // Example integration with advanced configuration scenarios:
            .UseSentry(options =>
            {
                // The parameter 'options' here has values populated through the configuration system.
                // That includes 'appsettings.json', environment variables and anything else
                // defined on the ConfigurationBuilder.
                // See: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-2.1&tabs=basicconfiguration
                // Tracks the release which sent the event and enables more features: https://docs.sentry.io/learn/releases/
                // If not explicitly set here, the SDK attempts to read it from: AssemblyInformationalVersionAttribute and AssemblyVersion
                // TeamCity: %build.vcs.number%, VSTS: BUILD_SOURCEVERSION, Travis-CI: TRAVIS_COMMIT, AppVeyor: APPVEYOR_REPO_COMMIT, CircleCI: CIRCLE_SHA1
                options.Release = "e386dfd"; // Could also be any format, such as: 2.0, or however version of your app is

                options.MaxBreadcrumbs = 200;

                options.EnableTracing = true;

                // Call GET /home/block/true to see this in action
                options.CaptureBlockingCalls = true;

                // Set a proxy for outgoing HTTP connections
                options.HttpProxy = null; // new WebProxy("https://localhost:3128");

                // Example: Disabling support to compressed responses:
                options.DecompressionMethods = DecompressionMethods.None;

                options.MaxQueueItems = 100;
                options.ShutdownTimeout = TimeSpan.FromSeconds(5);

                // Configures the root scope
                options.ConfigureScope(s => s.SetTag("Always sent", "this tag"));
            })
            .Build();
}
