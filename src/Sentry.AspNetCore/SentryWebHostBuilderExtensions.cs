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

            // The earliest we can hook the SDK initialization code with the framework
            // Initialization happens at a later time depending if the default MEL backend is enabled or not.
            // In case the logging backend was replaced, init happens later, at the StartupFilter
            builder.ConfigureLogging((context, logging) =>
            {
                // Configuration should be built. Bind Sentry values:
                context.Configuration.GetSection("Sentry").Bind(aspnetOptions);
                configureOptions?.Invoke(aspnetOptions);

                // The earliest we can resolve HostingEnvironment
                // Make sure this runs before the user defined callbacks
                aspnetOptions.ConfigureOptionsActions.Insert(0, o =>
                {
                    aspnetOptions.SentryOptions = o;

                    if (!string.IsNullOrWhiteSpace(aspnetOptions.Dsn))
                    {
                        o.Dsn = new Dsn(aspnetOptions.Dsn);
                    }

                    o.Release = aspnetOptions.Release;

                    o.Environment
                        = aspnetOptions.Environment
                          ?? context.HostingEnvironment?.EnvironmentName;
                });

                // Prepare MEL provider in case MEL backend is going to be used.
                logging.AddFilter<SentryLoggerProvider>(
                    "Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware",
                    LogLevel.None);

                logging.AddSentry(logOptions => aspnetOptions.Apply(logOptions));
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
