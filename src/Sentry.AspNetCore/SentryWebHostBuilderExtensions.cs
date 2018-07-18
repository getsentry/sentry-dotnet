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
            => builder.UseSentry(o => o.Init(i =>
            {
                if (!Dsn.IsDisabled(dsn))
                {
                    // If it's invalid, let ctor throw
                    i.Dsn = new Dsn(dsn);
                }
            }));

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
            var aspnetOptions = new SentryAspNetCoreOptions();

            // Default aspnetOptions to sentryOptions configuration:
            aspnetOptions.ConfigureOptionsActions.Add(o =>
            {
                if (!string.IsNullOrWhiteSpace(aspnetOptions.Dsn))
                {
                    o.Dsn = new Dsn(aspnetOptions.Dsn);
                }

                o.Environment = aspnetOptions.Environment ??
                                Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

                o.Release = aspnetOptions.Release;
            });

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
