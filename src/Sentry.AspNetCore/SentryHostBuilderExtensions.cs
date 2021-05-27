using System;
using System.ComponentModel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;
using Sentry.AspNetCore;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.Hosting
{
    /// <summary>
    /// Extension methods to <see cref="IHostBuilder"/>
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class SentryHostBuilderExtensions
    {
        /// <summary>
        /// Uses Sentry integration.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <returns></returns>
        public static IHostBuilder UseSentry(this IHostBuilder builder)
            => UseSentry(builder, (Action<SentryAspNetCoreOptions>?)null);

        /// <summary>
        /// Uses Sentry integration.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="dsn">The DSN.</param>
        /// <returns></returns>
        public static IHostBuilder UseSentry(this IHostBuilder builder, string dsn)
            => builder.UseSentry(o => o.Dsn = dsn);

        /// <summary>
        /// Uses Sentry integration.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="configureOptions">The configure options.</param>
        /// <returns></returns>
        public static IHostBuilder UseSentry(
            this IHostBuilder builder,
            Action<SentryAspNetCoreOptions>? configureOptions)
            => builder.UseSentry((_, options) => configureOptions?.Invoke(options));

        /// <summary>
        /// Uses Sentry integration.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="configureOptions">The configure options.</param>
        /// <returns></returns>
        public static IHostBuilder UseSentry(
            this IHostBuilder builder,
            Action<HostBuilderContext, SentryAspNetCoreOptions>? configureOptions)
            => builder.UseSentry((context, sentryBuilder) =>
                sentryBuilder.AddSentryOptions(options => configureOptions?.Invoke(context, options)));

        /// <summary>
        /// Uses Sentry integration.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="configureSentry">The Sentry builder.</param>
        /// <returns></returns>
        public static IHostBuilder UseSentry(
            this IHostBuilder builder,
            Action<ISentryBuilder>? configureSentry) =>
            builder.UseSentry((_, sentryBuilder) => configureSentry?.Invoke(sentryBuilder));

        /// <summary>
        /// Uses Sentry integration.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="configureSentry">The Sentry builder.</param>
        /// <returns></returns>
        public static IHostBuilder UseSentry(
            this IHostBuilder builder,
            Action<HostBuilderContext, ISentryBuilder>? configureSentry)
        {
            // The earliest we can hook the SDK initialization code with the framework
            // Initialization happens at a later time depending if the default MEL backend is enabled or not.
            // In case the logging backend was replaced, init happens later, at the StartupFilter
            _ = builder.ConfigureLogging((context, logging) =>
            {
                logging.AddConfiguration();

                var section = context.Configuration.GetSection("Sentry");
                _ = logging.Services.Configure<SentryAspNetCoreOptions>(section);

                _ = logging.Services
                    .AddSingleton<IConfigureOptions<SentryAspNetCoreOptions>, SentryAspNetCoreOptionsSetup>();
                _ = logging.Services.AddSingleton<ILoggerProvider, SentryAspNetCoreLoggerProvider>();

                _ = logging.AddFilter<SentryAspNetCoreLoggerProvider>(
                    "Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware",
                    LogLevel.None);

                var sentryBuilder = logging.Services.AddSentry();
                configureSentry?.Invoke(context, sentryBuilder);
            });

            _ = builder.ConfigureServices((c, s) => _ = s.AddTransient<IStartupFilter, SentryStartupFilter>());

            return builder;
        }
    }
}
