using System;
using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;
using Sentry.AspNetCore;

// ReSharper disable once CheckNamespace
namespace Microsoft.AspNetCore.Hosting
{
    /// <summary>
    /// Extension methods to <see cref="IWebHostBuilder"/>
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class SentryWebHostBuilderExtensions
    {
        /// <summary>
        /// Uses Sentry integration.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <returns></returns>
        public static IWebHostBuilder UseSentry(this IWebHostBuilder builder)
            => UseSentry(builder, (Action<SentryAspNetCoreOptions>)null);

        /// <summary>
        /// Uses Sentry integration.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="dsn">The DSN.</param>
        /// <returns></returns>
        public static IWebHostBuilder UseSentry(this IWebHostBuilder builder, string dsn)
            => builder.UseSentry(o => o.Dsn = dsn);

        /// <summary>
        /// Uses Sentry integration.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="configureOptions">The configure options.</param>
        /// <returns></returns>
        public static IWebHostBuilder UseSentry(
            this IWebHostBuilder builder,
            Action<SentryAspNetCoreOptions> configureOptions)
        {
            // The earliest we can hook the SDK initialization code with the framework
            // Initialization happens at a later time depending if the default MEL backend is enabled or not.
            // In case the logging backend was replaced, init happens later, at the StartupFilter
            builder.ConfigureLogging((context, logging) =>
            {
                logging.AddConfiguration();

                if (configureOptions != null)
                {
                    logging.Services.Configure(configureOptions);
                }

                logging.Services.AddSingleton<IConfigureOptions<SentryAspNetCoreOptions>, SentryAspNetCoreOptionsSetup>();
                logging.Services.AddSingleton<ILoggerProvider, SentryAspNetCoreLoggerProvider>();

                logging.AddFilter<SentryAspNetCoreLoggerProvider>(
                    "Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware",
                    LogLevel.None);

                logging.Services.AddSentry();
            });

            builder.ConfigureServices(c =>
            {
                c.AddTransient<IStartupFilter, SentryStartupFilter>();
            });

            return builder;
        }
    }
}
