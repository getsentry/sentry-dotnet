using System.ComponentModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;

namespace Sentry.AspNetCore
{
    /// <summary>
    /// Extension methods for <see cref="ILoggingBuilder"/>
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class SentryLoggingBuilderExtensions
    {
        public static ISentryBuilder AddSentry(this ILoggingBuilder builder, IConfiguration configuration)
        {
            builder.AddConfiguration();

            var section = configuration.GetSection("Sentry");
            _ = builder.Services.Configure<SentryAspNetCoreOptions>(section);

            if(!builder.Services.IsRegistered<IConfigureOptions<SentryAspNetCoreOptions>, SentryAspNetCoreOptionsSetup>())
            {
                _ = builder.Services
                    .AddSingleton<IConfigureOptions<SentryAspNetCoreOptions>, SentryAspNetCoreOptionsSetup>();
            }

            if(!builder.Services.IsRegistered<ILoggerProvider, SentryAspNetCoreLoggerProvider>())
            {
                _ = builder.Services.AddSingleton<ILoggerProvider, SentryAspNetCoreLoggerProvider>();
            }

            _ = builder.AddFilter<SentryAspNetCoreLoggerProvider>(
                "Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware",
                LogLevel.None);

            return builder.Services.AddSentry();
        }
    }
}
