using System;
using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;
using Sentry.AspNetCore;
using Sentry.AspNetCore.Grpc;

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
        /// Uses Sentry gRPC integration.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <returns></returns>
        public static IWebHostBuilder UseSentryGrpc(this IWebHostBuilder builder)
            => UseSentryGrpc(builder, (Action<SentryAspNetCoreGrpcOptions>?)null);

        /// <summary>
        /// Uses Sentry gRPC integration.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="dsn">The DSN.</param>
        /// <returns></returns>
        public static IWebHostBuilder UseSentryGrpc(this IWebHostBuilder builder, string dsn)
            => builder.UseSentryGrpc(o => o.Dsn = dsn);

        /// <summary>
        /// Uses Sentry gRPC integration.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="configureOptions">The configure options.</param>
        /// <returns></returns>
        public static IWebHostBuilder UseSentryGrpc(
            this IWebHostBuilder builder,
            Action<SentryAspNetCoreGrpcOptions>? configureOptions)
            => builder.UseSentryGrpc((context, options) => configureOptions?.Invoke(options));

        /// <summary>
        /// Uses Sentry gRPC integration.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="configureOptions">The configure options.</param>
        /// <returns></returns>
        public static IWebHostBuilder UseSentryGrpc(
            this IWebHostBuilder builder,
            Action<WebHostBuilderContext, SentryAspNetCoreGrpcOptions>? configureOptions)
        {
            // The earliest we can hook the SDK initialization code with the framework
            // Initialization happens at a later time depending if the default MEL backend is enabled or not.
            // In case the logging backend was replaced, init happens later, at the StartupFilter
            _ = builder.ConfigureLogging((context, logging) =>
            {
                logging.AddConfiguration();

                var section = context.Configuration.GetSection("Sentry");
                _ = logging.Services.Configure<SentryAspNetCoreGrpcOptions>(section);

                if (configureOptions != null)
                {
                    _ = logging.Services.Configure<SentryAspNetCoreGrpcOptions>(options =>
                    {
                        configureOptions(context, options);
                    });
                }

                _ = logging.Services
                    .AddSingleton<IConfigureOptions<SentryAspNetCoreGrpcOptions>, SentryAspNetCoreOptionsSetup>();
                _ = logging.Services.AddSingleton<ILoggerProvider, SentryAspNetCoreLoggerProvider>();

                _ = logging.AddFilter<SentryAspNetCoreLoggerProvider>(
                    "Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware",
                    LogLevel.None);

                _ = logging.Services.AddSentryGrpc();
            });

            return builder;
        }
    }
}
