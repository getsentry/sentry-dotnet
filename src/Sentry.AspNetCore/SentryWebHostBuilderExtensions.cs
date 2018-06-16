using System;
using System.ComponentModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sentry;
using Sentry.AspNetCore;
using Sentry.Extensions.Logging;

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
        /// Use Sentry's middleware to capture errors
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <returns></returns>
        public static IWebHostBuilder UseSentry(this IWebHostBuilder builder) => UseSentry(builder, (Action<SentryAspNetCoreOptions>)null);

        /// <summary>
        /// Use Sentry's middleware to capture errors
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="dsn">The DSN.</param>
        /// <returns></returns>
        public static IWebHostBuilder UseSentry(this IWebHostBuilder builder, string dsn)
        {
            return builder.UseSentry(o => o.Init(i => i.Dsn = new Dsn(dsn)));
        }

        public static IWebHostBuilder UseSentry(
            this IWebHostBuilder builder,
            Action<SentryAspNetCoreOptions> configureOptions)
        {
            var aspnetOptions = new SentryAspNetCoreOptions();

            builder.ConfigureLogging((context, logging) =>
            {
                context.Configuration.GetSection("Sentry").Bind(aspnetOptions);
                configureOptions?.Invoke(aspnetOptions);

                logging.AddFilter<SentryLoggerProvider>(
                    "Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware",
                    LogLevel.None);

                logging.AddSentry(o => aspnetOptions.Apply(o));
            });

            builder.ConfigureServices(c =>
            {
                c.AddSingleton(aspnetOptions);
                c.AddTransient<IStartupFilter, SentryStartupFilter>();

                c.AddSentry();
            });

            return builder;
        }
    }
}
