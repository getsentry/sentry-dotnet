using System;
using Microsoft.Extensions.DependencyInjection;

namespace Sentry.AspNetCore
{
    /// <summary>
    /// Extension methods for <see cref="ISentryBuilder"/>
    /// </summary>
    public static class SentryBuilderExtensions
    {
        /// <summary>
        /// Configure Sentry options
        /// </summary>
        /// <param name="builder">The Sentry builder</param>
        /// <param name="configureOptions">The configure options</param>
        /// <returns></returns>
        public static ISentryBuilder AddSentryOptions(this ISentryBuilder builder,
            Action<SentryAspNetCoreOptions>? configureOptions)
        {
            if (configureOptions != null)
            {
                builder.Services.Configure(configureOptions);
            }

            return builder;
        }
    }
}
