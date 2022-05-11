using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;
using Sentry.Extensions.Logging.Internal;

namespace Sentry.AspNetCore
{
    internal static class SentryLoggingBuilderExtensions
    {
        public static ISentryBuilder AddSentry(this ILoggingBuilder builder, IConfiguration configuration)
        {
            builder.AddConfiguration();

            var section = configuration.GetSection("Sentry");
            builder.Services.Configure<SentryAspNetCoreOptions>(section);

            builder.Services.TryAddExactSingleton<IConfigureOptions<SentryAspNetCoreOptions>, SentryAspNetCoreOptionsSetup>();
            builder.Services.TryAddExactSingleton<ILoggerProvider, SentryAspNetCoreLoggerProvider>();

            builder.AddFilter<SentryAspNetCoreLoggerProvider>(
                "Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware",
                LogLevel.None);

            return builder.Services.AddSentry();
        }
    }
}
