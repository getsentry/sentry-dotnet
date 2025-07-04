using System.Net;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Sentry.AspNetCore;
using Sentry.AspNetCore.Grpc;

namespace Sentry.Samples.AspNetCore.Grpc;

public static class Program
{
    public static void Main(string[] args)
    {
        BuildWebHost(args).Run();
    }

    public static IWebHost BuildWebHost(string[] args) =>
        WebHost.CreateDefaultBuilder(args)
            .UseShutdownTimeout(TimeSpan.FromSeconds(10))
            .ConfigureKestrel(options =>
            {
                // Setup a HTTP/2 endpoint without TLS due to macOS limitation.
                // https://docs.microsoft.com/en-us/aspnet/core/grpc/troubleshoot?view=aspnetcore-5.0#unable-to-start-aspnet-core-grpc-app-on-macos
                options.ListenLocalhost(5000, o => o.Protocols =
                    HttpProtocols.Http2);
            })
            .UseStartup<Startup>()

            // Example integration with advanced configuration scenarios:
            .UseSentry(builder =>
            {
                builder.AddGrpc();
                builder.AddSentryOptions(options =>
                {
#if !SENTRY_DSN_DEFINED_IN_ENV
                    // A DSN is required. You can set here in code, via the SENTRY_DSN environment variable or in your
                    // appsettings.json file.
                    // See https://docs.sentry.io/platforms/dotnet/guides/aspnetcore/#configure
                    options.Dsn = SamplesShared.Dsn;
#endif

                    // The parameter 'options' here has values populated through the configuration system.
                    // That includes 'appsettings.json', environment variables and anything else
                    // defined on the ConfigurationBuilder.
                    // See: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-2.1&tabs=basicconfiguration
                    // Tracks the release which sent the event and enables more features: https://docs.sentry.io/learn/releases/
                    // If not explicitly set here, the SDK attempts to read it from: AssemblyInformationalVersionAttribute and AssemblyVersion
                    // TeamCity: %build.vcs.number%, VSTS: BUILD_SOURCEVERSION, Travis-CI: TRAVIS_COMMIT, AppVeyor: APPVEYOR_REPO_COMMIT, CircleCI: CIRCLE_SHA1
                    options.Release =
                        "e386dfd"; // Could also be any format, such as: 2.0, or however version of your app is

                    options.TracesSampleRate = 1.0;

                    options.MaxBreadcrumbs = 200;

                    // Set a proxy for outgoing HTTP connections
                    options.HttpProxy = null; // new WebProxy("https://localhost:3128");

                    // Example: Disabling support to compressed responses:
                    options.DecompressionMethods = DecompressionMethods.None;

                    options.MaxQueueItems = 100;
                    options.ShutdownTimeout = TimeSpan.FromSeconds(5);

                    // Configures the root scope
                    options.ConfigureScope(s => s.SetTag("Always sent", "this tag"));
                });
            })
            .Build();
}
