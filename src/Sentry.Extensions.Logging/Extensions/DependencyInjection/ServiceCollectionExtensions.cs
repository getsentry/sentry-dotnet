using System;
using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Sentry.Extensibility;
using Sentry.Internal;

namespace Sentry.Extensions.Logging.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for <see cref="IServiceCollection"/>
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Sentry's services to the <see cref="IServiceCollection"/>
        /// </summary>
        /// <param name="services">The services.</param>
        /// <returns></returns>
        public static IServiceCollection AddSentry<TOptions>(this IServiceCollection services)
            where TOptions : SentryLoggingOptions, new()
        {
            services.TryAddSingleton<SentryOptions>(
                c => c.GetRequiredService<IOptions<TOptions>>().Value);

            services.TryAddTransient<ISentryClient>(c => c.GetRequiredService<IHub>());
            services.TryAddTransient(c => c.GetRequiredService<Func<IHub>>()());

            services.TryAddSingleton<Func<IHub>>(c =>
            {
                var options = c.GetRequiredService<IOptions<TOptions>>().Value;

                if (options.InitializeSdk)
                {
                    var hub = SentrySdk.InitHub(options);
                    SentrySdk.UseHub(hub);
                }

                return () => HubAdapter.Instance;
            });

            return services;
        }
    }
}
