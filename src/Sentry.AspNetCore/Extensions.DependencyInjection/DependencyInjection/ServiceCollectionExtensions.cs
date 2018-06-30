using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sentry;
using Sentry.AspNetCore;
using Sentry.Extensibility;

// ReSharper disable once CheckNamespace -- Discoverability
namespace Microsoft.Extensions.DependencyInjection
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
            services.TryAddSingleton<IUserFactory, DefaultUserFactory>();

            services
                .AddSingleton<IRequestPayloadExtractor, FormRequestPayloadExtractor>()
                // Last
                .AddSingleton<IRequestPayloadExtractor, DefaultRequestPayloadExtractor>();

            // If another Hub or Client wasn't registered by the app, always read the accessible through `SentrySdk`
            services.TryAddTransient(p => HubAdapter.Instance as IHub);
            services.TryAddTransient(p => HubAdapter.Instance as ISentryClient);

            return services;
        }
    }
}
