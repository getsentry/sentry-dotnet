using System.Net;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Sentry.AspNetCore;
using Sentry.AspNetCore.Grpc;
using Sentry.Samples.AspNetCore.Grpc;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost
    .UseShutdownTimeout(TimeSpan.FromSeconds(10))
    .ConfigureKestrel(options =>
    {
        // Setup a HTTP/2 endpoint without TLS due to macOS limitation.
        // https://docs.microsoft.com/en-us/aspnet/core/grpc/troubleshoot?view=aspnetcore-5.0#unable-to-start-aspnet-core-grpc-app-on-macos
        options.ListenLocalhost(5000, o => o.Protocols = HttpProtocols.Http2);
    })
    .UseSentry(sentryBuilder =>
    {
        sentryBuilder.AddGrpc();
        sentryBuilder.AddSentryOptions(options =>
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
            // Tracks the release which sent the event and enables more features: https://docs.sentry.io/learn/releases/
            // If not explicitly set here, the SDK attempts to read it from: AssemblyInformationalVersionAttribute and AssemblyVersion
            options.Release = "e386dfd";

            // Enable performance monitoring
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
    });

// Services
builder.Services.AddGrpcReflection();
builder.Services.AddGrpc();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Map gRPC endpoints
app.MapGrpcService<GameService>();
if (app.Environment.IsDevelopment())
{
    app.MapGrpcReflectionService();
}

app.Run();
