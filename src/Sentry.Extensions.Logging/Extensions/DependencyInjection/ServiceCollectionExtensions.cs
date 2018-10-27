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

            services.TryAddSingleton<OptionalHub>();

            services.TryAddSingleton(c =>
            {
                var options = c.GetRequiredService<IOptions<TOptions>>().Value;

                IHub hub;
                if (options.InitializeSdk)
                {
                    hub = c.GetRequiredService<OptionalHub>();
                }
                else
                {
                    // Access to whatever the SentrySdk points to (disabled or initialized via SentrySdk.Init)
                    hub = HubAdapter.Instance;
                }

                return hub;
            });

            services.TryAddSingleton<ISentryClient>(c => c.GetService<IHub>());

            return services;
        }
    }
}
