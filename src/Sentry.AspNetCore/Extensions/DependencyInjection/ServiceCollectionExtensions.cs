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
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Sentry's services to the <see cref="IServiceCollection"/>
        /// </summary>
        /// <param name="services">The services.</param>
        /// <returns></returns>
        public static ISentryBuilder AddSentry(this IServiceCollection services)
        {
            services.AddSingleton<ISentryEventProcessor, AspNetCoreEventProcessor>();
            services.TryAddSingleton<IUserFactory, DefaultUserFactory>();

            services
                    .AddSingleton<IRequestPayloadExtractor, FormRequestPayloadExtractor>()
                    // Last
                    .AddSingleton<IRequestPayloadExtractor, DefaultRequestPayloadExtractor>();

            services.AddSentry<SentryAspNetCoreOptions>();

            return new SentryAspNetCoreBuilder(services);
        }
    }
}
