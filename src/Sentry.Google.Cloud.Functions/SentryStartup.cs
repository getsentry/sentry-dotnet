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
using Sentry.Extensions.Logging;
using Sentry.Reflection;

namespace Google.Cloud.Functions.Framework;

/// <summary>
/// Starts up the GCP Function integration.
/// </summary>
public class SentryStartup : FunctionsStartup
{
    private const string SdkName = "sentry.dotnet.google-cloud-function";

    private static readonly SdkVersion NameAndVersion = typeof(SentryStartup).Assembly.GetNameAndVersion();

    /// <summary>
    /// Configure Sentry logging.
    /// </summary>
    public override void ConfigureLogging(WebHostBuilderContext context, ILoggingBuilder logging)
    {
        base.ConfigureLogging(context, logging);
        logging.AddConfiguration(context.Configuration);

        // TODO: refactor this with SentryWebHostBuilderExtensions
        var section = context.Configuration.GetSection("Sentry");
        logging.Services.AddSingleton<IConfigureOptions<SentryAspNetCoreOptions>>(
            _ => new SentryAspNetCoreOptionsSetup(section)
            );
        logging.Services.AddSingleton<IConfigureOptions<SentryAspNetCoreOptions>>(
            _ => new SentryHostLoggingOptionsSetup<SentryAspNetCoreOptions>(context.Configuration)
            );

        logging.Services.Configure<SentryAspNetCoreOptions>(options =>
        {
            // Make sure all events are flushed out
            options.FlushBeforeRequestCompleted = true;

            options.SetSdk(SdkName, NameAndVersion);

            // K_SERVICE is where the name of the FAAS is stored.
            // It will return null if GCP Function is running locally.
            var serviceName = options.SettingLocator.GetEnvironmentVariable("K_SERVICE");
            options.TransactionNameProvider = _ => serviceName;

            // Append revision from environment variable to release, unless release has already been set.
            if (string.IsNullOrWhiteSpace(options.Release))
            {
                var revision = options.SettingLocator.GetEnvironmentVariable("K_REVISION");
                if (!string.IsNullOrWhiteSpace(revision))
                {
                    var release = options.SettingLocator.GetRelease();
                    options.Release = $"{release}+{revision}";
                }
            }
        });

        logging.Services.AddSingleton<ILoggerProvider, SentryAspNetCoreLoggerProvider>();
        logging.Services.AddSingleton<ILoggerProvider, SentryAspNetCoreStructuredLoggerProvider>();

        // Add a delegate rule in order to ignore Configuration like "appsettings.json" and "appsettings.{HostEnvironment}.json"
        logging.AddFilter<SentryAspNetCoreLoggerProvider>(static (string? categoryName, LogLevel logLevel) =>
        {
            return categoryName is null
                   || categoryName != "Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware";
        });
        // Add non-delegate rules in order to respect Configuration like "appsettings.json" and "appsettings.{HostEnvironment}.json"
        logging.AddFilter<SentryAspNetCoreStructuredLoggerProvider>("Sentry.ISentryClient", LogLevel.None);
        logging.AddFilter<SentryAspNetCoreStructuredLoggerProvider>("Sentry.AspNetCore.SentryMiddleware", LogLevel.None);

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
        services.AddTransient<SentryMiddleware>();
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

    private class SentryGoogleCloudFunctionsMiddleware
    {
        private readonly RequestDelegate _next;

        public SentryGoogleCloudFunctionsMiddleware(RequestDelegate next) => _next = next;

        /// <summary>
        /// Handles the <see cref="HttpContext"/>.
        /// </summary>
        public Task InvokeAsync(HttpContext httpContext) => _next(httpContext);
    }
}
