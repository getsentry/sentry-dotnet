using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sentry.AspNetCore;
using Sentry.Extensibility;
using Sentry.Extensions.Logging.Extensions.DependencyInjection;

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
            _ = services.AddSingleton<ISentryEventProcessor, AspNetCoreEventProcessor>();
            services.TryAddSingleton<IUserFactory, DefaultUserFactory>();

            _ = services
                    .AddSingleton<IRequestPayloadExtractor, FormRequestPayloadExtractor>()
                    // Last
                    .AddSingleton<IRequestPayloadExtractor, DefaultRequestPayloadExtractor>();

            _ = services.AddSentry<SentryAspNetCoreOptions>();

            return services;
        }
    }
}
