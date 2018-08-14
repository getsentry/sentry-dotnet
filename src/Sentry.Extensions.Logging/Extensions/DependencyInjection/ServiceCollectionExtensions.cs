using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sentry.Extensibility;

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
            services.TryAddTransient(p => HubAdapter.Instance as IHub);
            services.TryAddTransient(p => HubAdapter.Instance as ISentryClient);

            return services;
        }
    }
}
