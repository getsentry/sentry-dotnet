using Google.Cloud.Functions.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sentry;
using Sentry.AspNetCore;
using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Reflection;

namespace Google.Cloud.Functions.Framework;

/// <summary>
/// Starts up the GCP Function integration.
/// </summary>
public class SentryStartup : FunctionsStartup
{
    /// <summary>
    /// Configure Sentry logging.
    /// </summary>
    public override void ConfigureLogging(WebHostBuilderContext context, ILoggingBuilder logging)
    {
        base.ConfigureLogging(context, logging);
        logging.AddConfiguration(context.Configuration);

        logging.Services.AddSingleton<ISentryEventProcessor, SentryGoogleCloudFunctionEventProcessor>();

        ReleaseLocator.FromEnvironmentLazy = new Lazy<string?>(() =>
        {
            var environmentRelease = ReleaseLocator.LocateFromEnvironment();
            if (environmentRelease != null &&
                Environment.GetEnvironmentVariable("K_REVISION") is { } revision)
            {
                environmentRelease = $"{environmentRelease}+{revision}";
            }
            return environmentRelease;
        });

        // TODO: refactor this with SentryWebHostBuilderExtensions
        var section = context.Configuration.GetSection("Sentry");
        logging.Services.Configure<SentryAspNetCoreOptions>(section);

        logging.Services.Configure<SentryAspNetCoreOptions>(options =>
        {
            // Make sure all events are flushed out
            options.FlushBeforeRequestCompleted = true;
        });

        logging.Services.AddSingleton<IConfigureOptions<SentryAspNetCoreOptions>, SentryAspNetCoreOptionsSetup>();
        logging.Services.AddSingleton<ILoggerProvider, SentryAspNetCoreLoggerProvider>();

        logging.AddFilter<SentryAspNetCoreLoggerProvider>(
            "Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware",
            LogLevel.None);

        logging.Services.AddSentry();
    }

    /// <summary>
    /// Configure Sentry services.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="services"></param>
    public override void ConfigureServices(WebHostBuilderContext context, IServiceCollection services)
    {
        base.ConfigureServices(context, services);
        services.AddTransient<IStartupFilter, SentryStartupFilter>();
    }

    /// <summary>
    /// Configure Sentry middlewares./>.
    /// </summary>
    public override void Configure(WebHostBuilderContext context, IApplicationBuilder app)
    {
        base.Configure(context, app);
        app.UseMiddleware<SentryGoogleCloudFunctionsMiddleware>();
        app.UseSentryTracing();
    }

    private class SentryGoogleCloudFunctionEventProcessor : ISentryEventProcessor
    {
        private static readonly SdkVersion NameAndVersion
            = typeof(SentryStartup).Assembly.GetNameAndVersion();

        private static readonly string ProtocolPackageName = "nuget:" + NameAndVersion.Name;
        private const string SdkName = "sentry.dotnet.google-cloud-function";

        public SentryEvent Process(SentryEvent @event)
        {
            // Take over the SDK name since this wraps ASP.NET Core
            @event.Sdk.Name = SdkName;
            @event.Sdk.Version = NameAndVersion.Version;

            if (NameAndVersion.Version != null)
            {
                @event.Sdk.AddPackage(ProtocolPackageName, NameAndVersion.Version);
            }

            return @event;
        }
    }

    private class SentryGoogleCloudFunctionsMiddleware
    {
        private readonly RequestDelegate _next;

        public SentryGoogleCloudFunctionsMiddleware(RequestDelegate next) => _next = next;

        /// <summary>
        /// Handles the <see cref="HttpContext"/>.
        /// </summary>
        public async Task InvokeAsync(HttpContext httpContext)
        {
            httpContext.Features.Set<ISentryRouteName>(new SentryGoogleCloudFunctionsRouteName());
            await _next(httpContext).ConfigureAwait(false);
        }
    }

    private class SentryGoogleCloudFunctionsRouteName : ISentryRouteName
    {
        private static readonly Lazy<string?> RouteName = new(() => Environment.GetEnvironmentVariable("K_SERVICE"));

        // K_SERVICE is where the name of the FAAS is stored.
        // It'll return null. if GCP Function is running locally.
        public string? GetRouteName() => RouteName.Value;
    }
}
