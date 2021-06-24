using Google.Cloud.Functions.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sentry.AspNetCore;

namespace Google.Cloud.Functions.Framework
{
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

            // TODO: refactor this with SentryWebHostBuilderExtensions
            var section = context.Configuration.GetSection("Sentry");
            _ = logging.Services.Configure<SentryAspNetCoreOptions>(section);

            _ = logging.Services.Configure<SentryAspNetCoreOptions>(options =>
            {
                // Make sure all events are flushed out
                options.FlushOnCompletedRequest = true;
            });

            _ = logging.Services.AddSingleton<IConfigureOptions<SentryAspNetCoreOptions>, SentryAspNetCoreOptionsSetup>();
            _ = logging.Services.AddSingleton<ILoggerProvider, SentryAspNetCoreLoggerProvider>();

            _ = logging.AddFilter<SentryAspNetCoreLoggerProvider>(
                "Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware",
                LogLevel.None);

            _ = logging.Services.AddSentry();
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
    }
}
