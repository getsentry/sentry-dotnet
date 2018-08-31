using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Sentry.Extensibility;
using Sentry.Internal;

namespace Sentry.Extensions.Logging.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for <see cref="IServiceCollection"/>
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Sentry's services to the <see cref="IServiceCollection"/>
        /// </summary>
        /// <param name="services">The services.</param>
        /// <returns></returns>
        public static IServiceCollection AddSentry(this IServiceCollection services)
        {
            // If another Hub or Client wasn't registered by the app, always read the accessible through `SentrySdk`
            services.TryAddSingleton<IHub>(c =>
            {
                var options = c.GetRequiredService<IOptions<SentryLoggingOptions>>().Value;

                if (options.InitializeSdk)
                {
                    var hub = c.GetRequiredService<HubWrapper>();
                    var disposable = SentrySdk.UseHub(hub);
                    var lifetime = c.GetService<IApplicationLifetime>();
                    lifetime?.ApplicationStopped.Register(() => disposable.Dispose());
                    return hub;
                }

                // Access to whatever the static Hub points to (disabled or initialized via SentrySdk.Init)
                return HubAdapter.Instance;
            });

            return services;
        }
    }
}
